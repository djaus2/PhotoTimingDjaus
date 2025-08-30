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
        private readonly Image _image;

        public ImageAdorner(UIElement adornedElement, Image image) : base(adornedElement)
        {
            _image = image;
            AddVisualChild(_image);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _image;

        protected override Size MeasureOverride(Size constraint)
        {
            _image.Measure(constraint);
            return _image.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Position image relative to adorned element (e.g., to the right of a Line)
            _image.Arrange(new Rect(new Point(160, -20), _image.DesiredSize));
            return finalSize;
        }
    }

}
