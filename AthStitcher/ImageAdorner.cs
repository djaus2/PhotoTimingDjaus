using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;  // For Adorner, AdornerLayer
using System.Windows.Media;     // For Visual, TranslateTransform, etc.
using System.Windows.Media.Imaging; // For BitmapImage if you're loading an image



namespace AthStitcher
{
    public class ImageAdorner : Adorner
    {
        private readonly System.Windows.Controls.Image _image;

        public ImageAdorner(UIElement adornedElement, System.Windows.Controls.Image image) : base(adornedElement)
        {
            _image = image;
            AddVisualChild(_image);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _image;

        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            _image.Measure(constraint);
            return _image.DesiredSize;
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            // Position image relative to adorned element (e.g., to the right of a Line)
            _image.Arrange(new System.Windows.Rect(new System.Windows.Point(160, -20), _image.DesiredSize));
            return finalSize;
        }
    }

}
