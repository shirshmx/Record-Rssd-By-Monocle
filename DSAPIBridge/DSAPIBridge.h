// DSAPIBridge.h

#pragma once

#include "stdafx.h"

#include "DSAPI.h"
#include "DSAPIUtil.h"
#include "DSSampleCommon/Common.h"

#include <cassert>
#include <cctype>
#include <algorithm>
#include <sstream>
#include <memory>
#include <map>
#include <iostream>

using namespace System;
using namespace System::Runtime::InteropServices;

class DSAPI;
static std::shared_ptr<DSAPI> _dsapi;

#define DS4_CHECK_ERRORS(s)                                                                           \
    do                                                                                               \
		    {                                                                                                \
        if (!s)                                                                                      \
				        {                                                                                            \
            std::cerr << "\nDSAPI call failed at " << __FILE__ << ":" << __LINE__ << "!";            \
            std::cerr << "\n  status:  " << DSStatusString(_dsapi->getLastErrorStatus());           \
            std::cerr << "\n  details: " << _dsapi->getLastErrorDescription() << '\n' << std::endl; \
            std::cerr << "\n hit return to exit " << '\n' << std::endl;                              \
            getchar();                                                                               \
            exit(EXIT_FAILURE);                                                                      \
				        }                                                                                            \
		    } while (false);

static uint8_t _zImageRGB[480 * 360 * 3];

const int COLOR_SIZE = 1920 * 1080 * 4;
const int DEPTH_SIZE = 480 * 360 * 2;

namespace DSAPIBridge {

	typedef struct
	{
		double rotation[9];
		double translation[3];
		double color_translation[3];
		double non_rect_baseLine;

		DSCalibIntrinsicsNonRectified calib_non_rect_left;
		DSCalibIntrinsicsNonRectified calib_non_rect_right;
		DSCalibIntrinsicsRectified calib_rect_lr;

		DSCalibIntrinsicsRectified calib_rect_color;
	} DS4Calibration;

	typedef struct
	{

		bool colorAutoExposure;
		float colorExposure;
		float colorGain;
		float depthExposure;
		float depthGain;
		
	} DS4SensorProperty;

	public ref class DSAPIManaged
	{

	public:
		DSAPIManaged()
		{
			_dsapi = std::shared_ptr<DSAPI>(DSCreate(DS_DS4_PLATFORM), DSDestroy);

			_colorBuffer = gcnew array<Byte>(COLOR_SIZE);
			_depthBuffer = gcnew array<Byte>(DEPTH_SIZE);
			_depthPreviewBuffer = gcnew array<Byte>(480 * 360 * 3);
		}

		//wrappers
		void initializeDevice()
		{
			_color = _dsapi->accessThird();
			_hardware = _dsapi->accessHardware();

			DS4_CHECK_ERRORS(_dsapi->probeConfiguration());

		}

		void enableDepth(int32_t width, int32_t height, int32_t frame_rate)
		{
			DS4_CHECK_ERRORS(_dsapi->enableZ(true));
			DS4_CHECK_ERRORS(_dsapi->enableLeft(false));
			DS4_CHECK_ERRORS(_dsapi->enableRight(false));
			DS4_CHECK_ERRORS(_dsapi->setLRZResolutionMode(true, 480, 360, 30, DS_LUMINANCE8)); // Valid resolutions: 628x468, 480x360
			DS4_CHECK_ERRORS(_dsapi->enableLRCrop(true));
		}

		void enableColor(int width, int height, int frame_rate)
		{
			DS4_CHECK_ERRORS(_color->enableThird(true));
			DS4_CHECK_ERRORS(_color->setThirdResolutionMode(true, width, height, frame_rate, DS_BGRA8)); // Valid resolutions: 1920x1080, 640x480
		}

		void setDepthExposureAndGain(float exposure, float gain)
		{

			DS4_CHECK_ERRORS(_hardware->setImagerExposure(exposure, DS_BOTH_IMAGERS));
			DS4_CHECK_ERRORS(_hardware->setImagerGain(gain, DS_BOTH_IMAGERS));
		}

		void setColorExposureAndGain(float exposure, float gain)
		{
			DS4_CHECK_ERRORS(_hardware->setAutoExposure(DS_THIRD_IMAGER, false));
			DS4_CHECK_ERRORS(_hardware->setImagerExposure(exposure, DS_THIRD_IMAGER));
			DS4_CHECK_ERRORS(_hardware->setImagerGain(gain, DS_THIRD_IMAGER));
		}

		void getSensorProperty(void *inputPtr)
		{
			DS4SensorProperty *sensorProperty = (DS4SensorProperty *)inputPtr;

			_hardware->getAutoExposure(DS_THIRD_IMAGER, sensorProperty->colorAutoExposure);
			
			_hardware->getImagerExposure(sensorProperty->colorExposure, DS_THIRD_IMAGER);
			_hardware->getImagerExposure(sensorProperty->depthExposure, DS_BOTH_IMAGERS);

			_hardware->getImagerGain(sensorProperty->colorGain, DS_THIRD_IMAGER);
			_hardware->getImagerGain(sensorProperty->depthGain, DS_BOTH_IMAGERS);
		}

		void startCapture()
		{
			DS4_CHECK_ERRORS(_dsapi->startCapture());
		}

		array<Byte>^ getDepthImage()
		{
			int width = _dsapi->zWidth();
			int height = _dsapi->zHeight();

			int len = width * height * 2;

			_zImagePtr = _dsapi->getZImage();
			Marshal::Copy((IntPtr)_zImagePtr, _depthBuffer, 0, len);

			return _depthBuffer;
		}

		array<Byte>^ getDepthImageAsRGB()
		{
			const uint8_t nearColor[] = { 255, 0, 0 }, farColor[] = { 20, 40, 255 };

			if (!_zImagePtr) {
				_zImagePtr = _dsapi->getZImage();
			}

			ConvertDepthToRGBUsingHistogram(_zImagePtr, _dsapi->zWidth(), _dsapi->zHeight(), nearColor, farColor, _zImageRGB);

			int len = sizeof(_zImageRGB);
			Marshal::Copy((IntPtr)_zImageRGB, _depthPreviewBuffer, 0, len);

			return _depthPreviewBuffer;
		}

		array<Byte>^ getColorImage()
		{
			IntPtr colorPtr = (IntPtr)_color->getThirdImage();

			int color_height = _color->thirdHeight();
			int color_width = _color->thirdWidth();

			Marshal::Copy(colorPtr, _colorBuffer, 0, COLOR_SIZE);
			return _colorBuffer;
		}

		void grabFrame()
		{
			DS4_CHECK_ERRORS(_dsapi->grab());
		}
		
		void loadCalibartionInfo(void* inputPtr)
		{

			DS4Calibration *calibration = (DS4Calibration *)inputPtr;


			DSCalibRectParameters params;

			_dsapi->getCalibRectParameters(params);

			_dsapi->getCalibExtrinsicsRectLeftToRectRight(calibration->non_rect_baseLine);
			_dsapi->getCalibExtrinsicsNonRectLeftToNonRectRight(calibration->rotation, calibration->translation);

			_dsapi->getCalibIntrinsicsNonRectLeft(calibration->calib_non_rect_left);
			_dsapi->getCalibIntrinsicsNonRectRight(calibration->calib_non_rect_right);
			_dsapi->getCalibIntrinsicsRectLeftRight(calibration->calib_rect_lr);

			_color->getCalibIntrinsicsRectThird(calibration->calib_rect_color);
			_color->getCalibExtrinsicsZToRectThird(calibration->color_translation);
		}

	private:
		DSThird *_color;
		DSHardware *_hardware;

		uint16_t* _zImagePtr;
		array<Byte> ^_colorBuffer;
		array<Byte> ^_depthBuffer;
		array<Byte> ^_depthPreviewBuffer;
	};
}
