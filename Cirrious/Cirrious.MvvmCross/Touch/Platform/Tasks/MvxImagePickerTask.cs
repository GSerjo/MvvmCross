#region Copyright
// <copyright file="MvxImagePickerTask.cs" company="Cirrious">
// (c) Copyright Cirrious. http://www.cirrious.com
// This source is subject to the Microsoft Public License (Ms-PL)
// Please see license.txt on http://opensource.org/licenses/ms-pl.html
// All other rights reserved.
// </copyright>
// 
// Project Lead - Stuart Lodge, Cirrious. http://www.cirrious.com
#endregion

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Cirrious.MvvmCross.Interfaces.Platform.Tasks;
using Cirrious.MvvmCross.Touch.Interfaces;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Cirrious.MvvmCross.Touch.Platform.Tasks
{
    public class MvxImagePickerTask : MvxTouchTask, IMvxPictureChooserTask
    {
        private readonly CameraDelegate _cameraDelegate;
        private readonly UIImagePickerController _picker;
        private readonly IMvxTouchViewPresenter _presenter;

        public MvxImagePickerTask(IMvxTouchViewPresenter presenter)
        {
            _picker = new UIImagePickerController();
            _cameraDelegate = new CameraDelegate();
            _picker.Delegate = _cameraDelegate;
            _presenter = presenter;
        }

        #region IMvxPictureChooserTask Members

        public void ChoosePictureFromLibrary(int maxPixelDimension, int percentQuality, Action<Stream> pictureAvailable, Action assumeCancelled)
        {
            _picker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
            ChoosePictureCommon(maxPixelDimension, percentQuality, pictureAvailable, assumeCancelled);
        }

        public void TakePicture(int maxPixelDimension, int percentQuality, Action<Stream> pictureAvailable, Action assumeCancelled)
        {
            _picker.SourceType = UIImagePickerControllerSourceType.Camera;
            ChoosePictureCommon(maxPixelDimension, percentQuality, pictureAvailable, assumeCancelled);
        }

        #endregion

        private void ChoosePictureCommon(int maxPixelDimension, int percentQuality, 
                                         Action<Stream> pictureAvailable, Action assumeCancelled)
        {
            _cameraDelegate.Callback = (UIImage image, NSDictionary dictionary) =>
                                           {
                                               if (image != null)
                                               {
                                                   // resize the image
                                                   image = image.ImageToFitSize (new SizeF (maxPixelDimension, maxPixelDimension));
					
                                                   using (NSData data = image.AsJPEG ((float)((float)percentQuality/100.0)))
                                                   {
                                                       var byteArray = new byte [data.Length];
                                                       Marshal.Copy (data.Bytes, byteArray, 0, Convert.ToInt32 (data.Length));
						
                                                       var imageStream = new MemoryStream ();
                                                       imageStream.Write (byteArray, 0, Convert.ToInt32 (data.Length));
                                                       imageStream.Seek (0, SeekOrigin.Begin);
						
                                                       pictureAvailable (imageStream);
                                                       _picker.DismissModalViewControllerAnimated (true);
                                                   }
					
                                                   return;
                                               }
				
                                               assumeCancelled ();
                                               _picker.DismissModalViewControllerAnimated (true);
                                           };

            _presenter.PresentNativeModalViewController(_picker, true);
        }

        #region Nested type: CameraDelegate

        class CameraDelegate : UIImagePickerControllerDelegate
        {
            public Action<UIImage, NSDictionary> Callback { get; set; }
			
            public override void FinishedPickingImage (UIImagePickerController picker, UIImage image, NSDictionary editingInfo)
            {
                if (Callback != null)
                    Callback (image, editingInfo);
            }
			
            public override void Canceled (UIImagePickerController picker)
            {
                if (Callback != null)
                    Callback (null, null);
            }
        }

        #endregion
    }
}