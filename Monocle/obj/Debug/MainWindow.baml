   M S B A M L   `   `   `  ����  Monocle    Monocle.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   -   5          � Title$  Monocle��5   	    � WindowState$ 	Maximized=�5   	    � 	Resources 5         FlashAttack �� 
TargetName �� TargetProperty W� From	 W� To g� Duration g� DecelerationRatio  
FlashDecay  RadioButtonStyle	 �� 
TargetType
 �� Property �� Value �� 
TargetType �� 
Background �� Bullet �� Orientation ��Name	 �� X1	 �� X2	 �� Y1	 �� Y2 �� Triggers P� Property P� Value �� 
TargetName  	BoolToVis  DisableGreen �� Color��%E  &	        &	 �     &	     &	 �    &	 �    �� 5   
   .��5      W� $ flash��6   $ Opacity�6<   $ 0X�6`   $	 0.7X�6i   $ 0:0:0.1D�6r   $	 0.8Q�6�   5      �� 5   
   .��5      W� $ flash��6   $ Opacity�6<   $	 0.7X�6`   $ 0X�6k   $ 0:0:0.2D�6r   $	 0.3Q�6�   5      �� 5   
   "	 	�6)   .�5      �� 
 � ��
 �   �6+   �� 5      
 � ��$ 18(�6)   �� 5      
 � ��$ Light"�6+   �� 5      
 � ��$ Center=�62   �� 5      
 � �� 5      �� 5      " 	�6&   .R�5      �� 
 ����  5      �� 5   "   $ 
Horizontal=�6-   .+�5   &   � $��20��6+   $��20��66   .+�5   *   �� $��MainRect��64   $��20��6D   $��20��6O   
�������
���   �6h   �� 5    *   $ Line1_Selected��6/   $ 0��6G   $ 20��6N   $ 0��6V   $ 20��6]   
���   �$��2��6t   $y�Hidden=�6�   �� 5!   *   $ Line2_Selected��6/   $ 0��6G   $ 20��6N   $ 20��6V   $ 0��6^   
���   �$��2��6t   $y�Hidden=�6�   5"   '   �� 5#   &   $��5,0,0,0q�67   $��Center=�6H   5$   #   5&       5'      P� 5(       �@J�IsMouseOver . . �5)   "   �� $ Line1_Selected��6)   
 � y�$ Visible=�6[   �� 5*   "   $ Line2_Selected��6)   
 � y�$ Visible=�6[   5+      P� 5,       �@d�	IsChecked$
 trueY�6;   . �5-   "   �� $ Line1_Selected��6)   
 � y�$ Visible=�6[   �� 5.   "   $ Line2_Selected��6)   
 � y�$ Visible=�6[   5/      51      53      54      �� 55   
   6'   �� 56   
   $ 
LightGreen��6/   57      .��58      �  � ColumnDefinitions 59   
   �� 5:      $
��Auto �6   �� 5;      $��* �6   �� 5<      $	��220 �6   .+�5>   
   � $��0��6   .+�5?      �� -   5        $��canvas��5?      $��0��6#   �� 5@      -   5        $��flash��5@      
������� J� Opacity$ 0Q�62   �� 5A      $��1��6   $ Vertical=�6&   $��Stretch=�6=   .+�5B      �� -   5        $��cameraDummpy��5B      $y�	Collapsed=�6,   5C      �� 5D      -   5        $��spCamera��5D      $
��Left=�6)   $	��Top=�6D   $��20q�6\   .+�5E      �� $��Camera��6   $��15(�6*   $
��Bold"�68   6L   d� 5F      -   5        $��btnColor��5I      $	��150��5F      $��40��6+   $��25(�5G      $
��Bold"�6-   
���iii�
�������$��color��5I   /   
 � Tag$ Color��6?   $��0,4,0,4q�6K   6\   d� 5J      -   5        $��btnDepth��5L   3   $	��150��5J      $��40��6,   $��25(�5K      $
��Bold"�6-   
���iii�
�������$��depth��5L   C   $ Depth��6S   $��0,0,0,4q�6_    d� 	IsChecked$
 TrueY�6�   5M      �� 5[      $ Vertical=�6   $��20, 290, 20, 20q�60   .+�5\      �� $��Tracking Accuracy: ��6   $��15(�67   $
��Bold"�6E   6Y   �� 5]      -   5        $��lblSceneTrackingAccuracy��5]      $��40��68   $��15(�6F   $
��Bold"�6T   5^      �� 5`      -	   5        $��spDeviceParam��5`      $
��Left=�6.   $	��Top=�6I   $��20, 340, 20, 20q�6a   .+�5a      �� -
   5        $��lblFrameRate��5a      $��15(�6,   $
��Bold"�6:   �� 5b      $ 
Horizontal=�6   $��5q�66   .+�5c      �� $��Depth Exposure:��6    $��15(�67   $
��Bold"�6E   6Y   �� 5d      -   5        $��inputImageExposure��5d      $��Center=�68   $��	10,0,0,10q�6U   $��40��6h   6v   5e      �� 5g      $ 
Horizontal=�6   $��5q�66   .+�5h      �� $��Depth Gain: ��6    $��15(�64   $
��Bold"�6B   6V   �� 5i      -   5        $��inputImageGain��5i      $��Center=�64   $��	10,0,0,10q�6Q   $��40��6d   6r   5j      �� 5l      $ 
Horizontal=�6   $��5q�66   .+�5m      �� $��Color Exposure:��6    $��15(�67   $
��Bold"�6E   6Y   �� 5n      -   5        $��colorImageExposure��5n      $��Center=�68   $��	10,0,0,10q�6U   $��40��6h   6v   5o      �� 5q      $ 
Horizontal=�6   $��5q�66   .+�5r      �� $��Color Gain: ��6    $��15(�64   $
��Bold"�6B   6V   �� 5s      -   5        $��colorImageGain��5s      $��Center=�64   $��	10,0,0,10q�6Q   $��40��6d   6r   5t      5v      �� 5x      -   5        $��spNewSession��5x      $��0,0,40,0q�6.   $��Center=�6@   $��Bottom=�6^   $ Vertical=�6y   .+�5y      �� -   5        $��spUploadPanel��5y   ;   $��Stretch=�6   $��0,10,0,0q�6P   $y�	Collapsed=�5z      .+�5{      �� $��Center=�6    $��12(�5|       ��5}       �� *Uploader.ProgressInMB+y�5~       �� *Uploader.InProgress+ �� 	Converter# [ � 5        ProgressBarStyle1#��[ 6"    � IsIndeterminate . $��0,3,0,3q�6e   y�5�   "   �� *Compressing+# [ � 5�      #��[ 6"   $��0,3,0,3q�5�   s     � Value  6:   �� *Uploader.PercentComplete+! �� Mode$! OneWay=�y�5�   :   �� *Uploader.InProgress+" �� FallbackValue$" Hidden��# [ �� 5�      $��Center=�6   $��Center=�6;   $��50��6W   $��15(�5�   ;   $
��Bold"�5�   ;   $��0q�5�   ;   # �� 
IsReadOnly# . y�5�   ;   �� *Uploader.Waiting+  InverseBoolToVis# [ ��5�   ;   �� *Uploader.Result+  UploadResultToMsg# [ $! OneWay=�5�      �� 5�      -   5        $��spScanOperation��5�      $��Center=�64   $y�	Collapsed=�6R   .+�5�      �� -   5        $��18(�5�      $
��Bold"�6+   
���iii�
�������$	��150��5�      $��40��6)   $��Center=�65   $��10q�6R   $��Open Directory��6^   5�      5�      5�      � 5�   
   $��1��6   $��Stretch=�6   $ � RowDefinitions$ 5�      �� 5�      $
��auto �6    �� 5�      $��* �6    .+�5�      �� $��3q�6   $��3��6    $��0��64   
�������$��3,3,3,3q�5�   )   % �� CornerRadius$% 3��6C   $��Stretch=�5�      $��Stretch=�6-   ._�5�      �  5�      �� 5�      $
��auto �6+   �� 5�      $��* �6+   .+�5�      �� -   5        $��spInputScan��5�   1   $��0��6!   $ 
Horizontal=�6D   $��	10,0,10,0q�6]   $��Stretch=�6p   $y�Visible=�6�   .+�5�      �� -   5        $��spNameGender��5�   %   $��10,10,30,10q�69   $��Stretch=�6N   $��Stretch=�6m   $ Vertical=�6�   .+�5�      �� $ 
Horizontal=�6)   .+�5�   "   �� $��Name: ��6,   $��20(�6:   $
��Bold"�6H   6\   �� 5�   "   -   5        $��	inputName��5�   7   $��0��6*   $��20(�6I   $��Center=�6W   $��	10,0,0,10q�6t   $	��120��6�   5�      �� 5�      $ 
Horizontal=�6)   .+�5�   "   �� $��Gender: ��6,   $��20(�6<   $
��Bold"�6J   6^   	� 5�   "   -   5        $��rbMale��5�   .   $��0,0,10,0q�6=   $
 TrueY�6O   #��[ 6y   .��5�   &   �� $
��Male��60   $��18(�6<   $
��Bold"�6J   $��29��6\   $��58��6h   6u   5�   #   	� 5�   "   -   5        $��rbFemale��5�   .   #��[ 6W   .��5�   &   �� $��Female��60   $��20(�6>   $
��Bold"�6L   6`   5�   #   5�      5�      �� 5�      -   5        $��spHeightWeight��5�   :   $��15,10,30,10q�6%   $��Stretch=�6P   $��Stretch=�6o   $ Vertical=�6�   .+�5�      �� $ 
Horizontal=�6)   .+�5�   "   �� $��Height(cm): ��6,   $��20(�6@   $
��Bold"�6N   6b   �� 5�   "   -   5        $��inputHeight��5�   7   $��0��6*   $��20(�6J   $��Center=�6X   $��	10,0,0,10q�6u   $��80��6�   5�      �� 5�      $ 
Horizontal=�6)   .+�5�   "   �� $��Weight(kg): ��6,   $��20(�6@   $
��Bold"�6N   6b   �� 5�   "   -   5        $��inputWeight��5�   7   $��0��6*   $��20(�6K   $��Center=�6Y   $��	10,0,0,10q�6v   $��80��6�   5�      5�      5�      �� 5�      -   5        $��spSessionInfo��5�   1   $��0��6!   $ 
Horizontal=�6F   $��5,0,10,0q�6_   $��Stretch=�6q   $y�	Collapsed=�6�   .+�5�      �� $��	5,10,30,5q�6%   $��Stretch=�68   $��Stretch=�6W   $ Vertical=�6s   .+�5�      �� $ 
Horizontal=�6)   .+�5�   "   �� $��Name: ��6,   $��20(�6:   $
��Bold"�6H   6\   �� 5�   "   $��20(�6K   $
��Bold"�6Y   $��Center=�6k   $��10,0,0,5q�6�   $��80��6�   ��6,   �� *Metadata.Name+6�   5�      �� 5�      $ 
Horizontal=�6)   .+�5�   "   �� $��Gender: ��6,   $��20(�6<   $
��Bold"�6J   6^   �� 5�   "   $��20(�6N   $
��Bold"�6\   $��Center=�6n   $��10,0,0,5q�6�   $��80��6�   ��6-   �� *Metadata.Gender+6�   5�      5�      �� 5�      -   5        $��spSessionHeightWeightInfo��5�   9   $��
0,10,30,10q�6%   $��Stretch=�6Z   $��Stretch=�6y   $ Vertical=�6�   .+�5�      �� $ 
Horizontal=�6)   .+�5�   "   �� $��Height(cm): ��6,   $��20(�6@   $
��Bold"�6N   6b   �� 5�   "   $��20(�6N   $
��Bold"�6\   ��6-   �� *Metadata.Height+6p   5�      �� 5�      $ 
Horizontal=�6)   $��0, 10, 0, 0q�6B   .+�5�   "   �� $��Weight(kg): ��6,   $��20(�6@   $
��Bold"�6N   6b   �� 5�   "   $��20(�6N   $
��Bold"�6\   ��6-   �� *Metadata.Weight+6p   5�      5�      �� 5�      $��10,10,30,10q�6%   $��Stretch=�6:   $��Stretch=�6Y   $ Vertical=�6u   .+�5�      �� $ 
Horizontal=�6)   .+�5�   "   �� $��Sweeps Recorded: ��6,   $��20(�6E   $
��Bold"�6S   6g   �� 5�   "   -   5        $��tbCapturedSweeps��5�   ,   $��0��6E   $��20(�6N   $
��Bold"�6\   6p   5�      �� 5�      $ 
Horizontal=�6)   $��0, 10, 0, 0q�6B   .+�5�   "   �� $��Last Sweep Frames: ��6,   $��20(�6G   $
��Bold"�6U   6i   �� 5�   "   -   5        $��tbCapturedFrames��5�   -   $��--��6E   $��20(�6O   $
��Bold"�6]   6q   5�      5�      5�      �� 5�      -   5        $��btnStartNew��5�   -   $��1��6   $��Center=�6c   $��25(�5�      $��	ExtraBold"�6+   
���iii�
�������$��60��5�   0   $	��230��6<   $��Right=�6H   $��0,0,10,0q�6d   $��New Session��5�      �� 5�      $��1��6!   $ 
Horizontal=�61   $��Right=�6J   .+�5�      �� -    5        $��btnEndSessionAndUpload��5�   !   $��Center=�6c   $��20(�5�      $��	ExtraBold"�6+   
���iii�
�������$��50��5�   0   $	��160��6<   $��Right=�6H   $��0,0,10,0q�6d   $y�	Collapsed=�5�      ��5�      �� 5�   "   "	 ��6(   .�5�   &   �� 
 � ��$ End & Upload��6@   & �� Triggers& 5�   &   �� 5�   *   ' �� Binding' 66   �� *Compressing+( �� Value( True6V   .a�5   .   �� 
 � ��$ Compressing...��6H   �� 5  .   
 � {� .  6J   5  +   �� 5  *   ' 66   �� ) �� Path$) Uploader.InProgress�( True6c   .a�5  .   �� 
 � ��$ Cancel��6H   5  +   �� 5  *   ' 66   �� $) Uploader.ReTryer.Started�( True6h   .a�5  .   �� 
 � ��$ 	Retry Now��6H   5	  +   5  #   5     �� 5     -!   5        $��btnEndAndStartNewSession��5  !   $��Center=�6`   $��20(�5     $��	ExtraBold"�6+   
���iii�
�������$��50��5  0   $	��150��6<   $��Right=�6H   $��0,0,10,0q�6d   $y�	Collapsed=�5     $	��End��64   5     5     5     �� 5     -"   5        $��camera��5  !   $��1��6   $��UniformToFill=�6/   5     � 5  
   $��2��6   $��0��6   $��Stretch=�6,   .+�5     ]� -#   5        $��CaptureControlPanel��5     $��Stretch=�63   $��0��6Q   .+�5!     �� -$   5        $��
btnCapture��5!  D   {�.  $��Stretch=�6V   $��Stretch=�6r   $��4q�6�   
������$��25(�6�    5"       FocusVisual* J� SnapsToDevicePixels+ �� StrokeDashArray 	 Button.Static.Background 
 Button.Static.Border  Button.MouseOver.Background  Button.MouseOver.Border  Button.Pressed.Background  Button.Pressed.Border  Button.Disabled.Background  Button.Disabled.Border  Button.Disabled.Foreground, ��Name- ��Name��%�  &	       &		 �     &	
 �     &	 �     &	     &	 6    &	 Y    &	 |    &	 �    &	 �    '�� �    7  7 	 7 
 7  7  7  7  7  �� 5#     .�5$     �� 
 � �� 5%  "   �� 5&  &   .R�5'  *   �� $��2q�64   * . #��� ��6Z   $��1��6�   $	+ 1 2R�6�   5(  '   5*     5+     �� 5,     $ 	#FFDDDDDD��6K   �� 5-     $ 	#FF707070��6G   �� 5.     $ 	#FFBEE6FD��6N   �� 5/     $ 	#FF3C7FB1��6J   �� 50     $ 	#FFC4E5F6��6L   �� 51     $ 	#FF2C628B��6H   �� 52     $ 
LightGreen��6M   �� 53     $ 	#FFADB2B5��6I   �� 54     $ 	#FF838383��6M   �� 55     "	 ��6    .�56     �� 
 �@�FocusVisualStyle8   6A   �� 57     
 � ��8  6;   �� 58     
 � ��8  6<   �� 59     
 � ��# � ��6;   �� 5:     
 � ��$ 1q�6@   �� 5;     
 � ��$ Center=�6K   �� 5<     
 � ��$ Center=�6I   �� 5=     
 � ��$ 1q�68   �� 5>     
 � �� 5?  "   �� 5@  &   " ��66   .R�5A  *   �� $, border��61   #��z��6A   #��z��6m   #��z��6�   * . ._�5B  .   �� $- contentPresenter��6?   |�.  #��z��6k   #��z��6�   ��. #* z* 6�   #��z��6(  5C  +    5D  *   P� 5E  .    �@��IsDefaulted . . �5F  2   �� $ border��6P   # � ��6d   
 � ��5G  /   P� 5H  .    � �� . . �5I  2   �� $ border��6O   8  
 � ��6c   �� 5J  2   $ border��6P   8  
 � ��6d   5K  /   P� 5L  .    � {� .  . �5M  2   �� $ border��6O   8  
 � ��6c   �� 5N  2   $ border��6P   8  
 � ��6d   �� 5O  2   $ contentPresenter��6[   8  
 � ��6y   5P  /   5R  '   5T     5U     5V     ��5W     �� 5X     $��25(�6$   $
��Bold"�62   $
��Wrap=�6D   . �� TextAlignment$. Center=�6X   Start a new session to begin6�   5[     5\     �� 5]     -%   5        $��spCaptureStats��5]     $ Vertical=�6/   $��
20,30,20,0q�6F   $	��Top=�6Z   $y�Visible=�6r   .+�5^     �� -&   5        $��lblCaptureCount��5^     $��17(�6/   $
��Bold"�6=   5_     �� 5a     -'   5        $��BtnRedoSweep��5a     $	��180��6)   $��60��65   $��20(�5b     $
��Bold"�6#   
���iii�
�������$��
20,30,20,0q�5c  (   $	��Top=�6<   $y�	Collapsed=�6T   .��5d     �� $
��Wrap=�6   $. Center=�60   Redo Last Sweep6X   5e     5f     5g     5h     