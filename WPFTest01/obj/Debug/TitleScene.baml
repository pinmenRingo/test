   M S B A M L   `   `   `  ����  	WPFTest01    WPFTest01.TitleScene   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5        N� Title$  
TitleScene��5       � 	Resources 5	         NewGameButtonStyle �� 
TargetType �� Property �� Value �� 
TargetType �Name J� RenderTransformOrigin'  !System.Windows.VisualStateManager   VisualStateGroups%  System.Windows.VisualStateGroup	   States   System.Windows.VisualState
   
Storyboard �� TargetProperty �� 
TargetName9  3System.Windows.Media.Animation.EasingDoubleKeyFrame O� KeyTime O� Value �� Triggers P� Property P� Value��%�  &	        �� 5
   
   " ��6+   .�5      ��  � �� 5      �� 5      " ��6&   .R�5      � $
 grid��6   $ 0.5,0.5&�6-   z�5      V� 5   "   .~�5   &   �� �� 5   &   �� 5   &   T� 5   &   5   #    5        5   "   CommonStates��.	 5   &     
Normal��62     5   &   	MouseOver��62     5   &   Pressed��.
 5   *   �� .��5   .   U� $V P(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)�6L   $
 grid��6�   .\�5   2     $ 0��6G   $
 0.95Q�6S   5   /   U� 5    .   $V P(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)�6L   $
 grid��6�   .\�5!   2     $ 0��6G   $
 0.95Q�6S   5"   /   5'   +   5(   '     5)   &   Disabled��62   5*   #   .+�5,      �� $	��250��6$   $��Bottom=�60   $
��Fill=�6K   $��Images/start.png��6Z   $��-1.25,-1.25,1.25,1.25q�6t   $��70��6�   $��Center=�6�   5-       5.      P� 5/       �@J�	IsFocused . 6;   P� 50       �@��IsDefaulted . 6=   P� 51       �@J�IsMouseOver . 6=   P� 52       � �� . 6;   P� 53       � {� .  6;   55      57      58      59      .��5;      � 
�������.+�5<   
   r� -   5         r�Name$ BGM_MediaElement��5<       r� Source$ Sounds/BGM.mp3?�61    r� LoadedBehavior$ Manual=�6I   6a   �� 5=   
   $��Images/bg.jpg��6   $
��Fill=�6'   �� 5>   
   $��Images/title.png��6   $��220,94,214,220.4q�6+   �� 5?   
   -   5        $��Button��5?      $��175,291,175,39.4q�67   #���   6Q   5@      5A      