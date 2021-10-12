using System.ComponentModel;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Views;
using CustomFrameApp.Android.Renderers;
using CustomFrameApp.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using FrameRenderer = Xamarin.Forms.Platform.Android.AppCompat.FrameRenderer;
using View = Android.Views.View;

[assembly: ExportRenderer(typeof(Border), typeof(BorderRenderer))]

namespace CustomFrameApp.Android.Renderers
{
    public sealed class BorderRenderer : FrameRenderer, View.IOnClickListener
    {
        private bool _isEnabled;
        private bool _inputTransparent;

        // ReSharper disable once UnusedMember.Global
        public BorderRenderer(Context context)
            : base(context)
        {
            // Keep this for Forms!
        }

        public override void Draw(Canvas canvas)
        {
            var border = (Border)Element;

            ClipCanvas(canvas, border);

            base.Draw(canvas);

            DrawStroke(canvas, border);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (!_isEnabled || _inputTransparent)
            {
                return false;
            }

            // Xamain Forms uses gesture manager to handle multiple gestures, so they observe move, and other gestures.. this makes the tap very short
            // Here we only need Click.. so let's restore the original Android behaviour
            if (e.Action == MotionEventActions.Up && IsPointInsideView(e.RawX, e.RawY, this))
            {
                return PerformClick();
            }

            return true;
        }

        public void OnClick(View v)
        {
            if (v == this && Element?.GestureRecognizers != null)
            {
                foreach (var tapGestureRecognizer in Element.GestureRecognizers.OfType<TapGestureRecognizer>())
                {
                    tapGestureRecognizer.SendTapped(Element);
                }
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                SetOnClickListener(null);
            }

            if (e.NewElement != null)
            {
                UpdateInputTransparent();
                UpdateIsEnabled();
            }

            Invalidate();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.InputTransparentProperty.PropertyName)
            {
                UpdateInputTransparent();
            }
            else if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
            {
                UpdateIsEnabled();
            }
            else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
            {
                UpdateBackground();
            }
            else if (e.PropertyName == Border.BorderColorProperty.PropertyName)
            {
                UpdateBorderColor();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SetOnClickListener(null);
            }

            base.Dispose(disposing);
        }

        private static bool IsPointInsideView(float x, float y, View view)
        {
            var location = new int[2];
            view.GetLocationOnScreen(location);
            var viewX = location[0];
            var viewY = location[1];

            // point is inside view bounds
            return (x > viewX && x < (viewX + view.Width)) &&
                    (y > viewY && y < (viewY + view.Height));
        }

        // Updating the background color since the dynamicresource update is not pushed to the native control,
        // since we have removed the base call.
        private void UpdateBackground()
        {
            if (Element == null)
            {
                return;
            }

            var color = Element.BackgroundColor.ToAndroid();
            Control.SetCardBackgroundColor(color);
        }

        private void UpdateBorderColor()
        {
            if (Element == null)
            {
                return;
            }

            var color = Element.BorderColor.ToAndroid();
            Control.SetBackgroundColor(color);
            Control.Invalidate();
        }

        private void UpdateInputTransparent()
        {
            if (Element == null)
            {
                return;
            }

            _inputTransparent = Element.InputTransparent;

            SetOnClickListener(!_inputTransparent ? this : null);
        }

        private void UpdateIsEnabled()
        {
            if (Element == null)
            {
                return;
            }

            _isEnabled = Element.IsEnabled;
        }

        private void ClipCanvas(Canvas canvas, Border border)
        {
            if (border.IsClippedToBounds)
            {
                return;
            }

            var cornerRadius = Context.ToPixels(border.CornerRadius);

            using (var path = new Path())
            {
                var top = Context.ToPixels(border.BorderThickness.Top);
                var right = Context.ToPixels(border.BorderThickness.Right);
                var bottom = Context.ToPixels(border.BorderThickness.Bottom);
                var left = Context.ToPixels(border.BorderThickness.Left);

                var hasCorner = cornerRadius > 0;

                RectF drawingRect = new RectF(hasCorner ? left / 2 : 0, hasCorner ? top / 2 : 0, hasCorner ? canvas.Width - (right / 2) : canvas.Width, hasCorner ? canvas.Height - (bottom / 2) : canvas.Height);

                RectF topLeftArcBound = new RectF();
                RectF topRightArcBound = new RectF();
                RectF bottomLeftArcBound = new RectF();
                RectF bottomRightArcBound = new RectF();

                topRightArcBound.Set(drawingRect.Right - (cornerRadius * 2), drawingRect.Top, drawingRect.Right, drawingRect.Top + (cornerRadius * 2));
                bottomRightArcBound.Set(drawingRect.Right - (cornerRadius * 2), drawingRect.Bottom - (cornerRadius * 2), drawingRect.Right, drawingRect.Bottom);
                bottomLeftArcBound.Set(drawingRect.Left, drawingRect.Bottom - (cornerRadius * 2), drawingRect.Left + (cornerRadius * 2), drawingRect.Bottom);
                topLeftArcBound.Set(drawingRect.Left, drawingRect.Top, drawingRect.Left + (cornerRadius * 2), drawingRect.Top + (cornerRadius * 2));

                path.Reset();

                path.MoveTo(drawingRect.Left + cornerRadius, drawingRect.Top);

                // draw top horizontal line
                path.LineTo(drawingRect.Right - cornerRadius, drawingRect.Top);

                // draw top-right corner
                if (cornerRadius > 0)
                {
                    path.ArcTo(topRightArcBound, -90, 90);
                }

                // draw right vertical line
                path.LineTo(drawingRect.Right, drawingRect.Bottom - cornerRadius);

                // draw bottom-right corner
                if (cornerRadius > 0)
                {
                    path.ArcTo(bottomRightArcBound, 0, 90);
                }

                // draw bottom horizontal line
                path.LineTo(drawingRect.Left - cornerRadius, drawingRect.Bottom);

                // draw bottom-left corner
                if (cornerRadius > 0)
                {
                    path.ArcTo(bottomLeftArcBound, 90, 90);
                }

                // draw left vertical line
                path.LineTo(drawingRect.Left, drawingRect.Top + cornerRadius);

                // draw top-left corner
                if (cornerRadius > 0)
                {
                    path.ArcTo(topLeftArcBound, 180, 90);
                }

                path.Close();

                canvas.ClipPath(path);
            }
        }

        private void DrawStroke(Canvas canvas, Border border)
        {
            var cornerRadius = Context.ToPixels(border.CornerRadius);

            var top = Context.ToPixels(border.BorderThickness.Top);
            var right = Context.ToPixels(border.BorderThickness.Right);
            var bottom = Context.ToPixels(border.BorderThickness.Bottom);
            var left = Context.ToPixels(border.BorderThickness.Left);

            using (var painttop = new Paint { AntiAlias = true })
            using (var paintright = new Paint { AntiAlias = true })
            using (var paintbottom = new Paint { AntiAlias = true })
            using (var paintleft = new Paint { AntiAlias = true })
            using (var style = Paint.Style.Stroke)
            {
                painttop.StrokeWidth = top;
                painttop.SetStyle(style);
                painttop.Color = border.BorderColor.ToAndroid();

                paintright.StrokeWidth = right;
                paintright.SetStyle(style);
                paintright.Color = border.BorderColor.ToAndroid();

                paintbottom.StrokeWidth = bottom;
                paintbottom.SetStyle(style);
                paintbottom.Color = border.BorderColor.ToAndroid();

                paintleft.StrokeWidth = left;
                paintleft.SetStyle(style);
                paintleft.Color = border.BorderColor.ToAndroid();

                var hasCorner = cornerRadius > 0;

                var drawingRect = new RectF(hasCorner ? left / 2 : 0, hasCorner ? top / 2 : 0, hasCorner ? canvas.Width - (right / 2) : canvas.Width, hasCorner ? canvas.Height - (bottom / 2) : canvas.Height);

                if (top > 0)
                {
                    canvas.DrawLine(drawingRect.Left, drawingRect.Top, drawingRect.Right - cornerRadius, drawingRect.Top, painttop);
                }

                if (top > 0 && cornerRadius > 0)
                {
                    var topRightArcBound = new RectF();
                    topRightArcBound.Set(drawingRect.Right - (cornerRadius * 2), drawingRect.Top, drawingRect.Right, drawingRect.Top + (cornerRadius * 2));
                    canvas.DrawArc(topRightArcBound, -90, 90, false, painttop);
                }

                if (right > 0)
                {
                    canvas.DrawLine(drawingRect.Right, drawingRect.Top + cornerRadius, drawingRect.Right, drawingRect.Bottom - cornerRadius, paintright);
                }

                if (right > 0 && cornerRadius > 0)
                {
                    var bottomRightArcBound = new RectF();
                    bottomRightArcBound.Set(drawingRect.Right - (cornerRadius * 2), drawingRect.Bottom - (cornerRadius * 2), drawingRect.Right, drawingRect.Bottom);
                    canvas.DrawArc(bottomRightArcBound, 0, 90, false, paintright);
                }

                if (bottom > 0)
                {
                    canvas.DrawLine(drawingRect.Right - cornerRadius, drawingRect.Bottom, drawingRect.Left + cornerRadius, drawingRect.Bottom, paintbottom);
                }

                if (bottom > 0 && cornerRadius > 0)
                {
                    var bottomLeftArcBound = new RectF();
                    bottomLeftArcBound.Set(drawingRect.Left, drawingRect.Bottom - (cornerRadius * 2), drawingRect.Left + (cornerRadius * 2), drawingRect.Bottom);
                    canvas.DrawArc(bottomLeftArcBound, 90, 90, false, paintbottom);
                }

                if (left > 0)
                {
                    canvas.DrawLine(drawingRect.Left, drawingRect.Bottom - cornerRadius, drawingRect.Left, drawingRect.Top + cornerRadius, paintleft);
                }

                if (left > 0 && cornerRadius > 0)
                {
                    var topLeftArcBound = new RectF();
                    topLeftArcBound.Set(drawingRect.Left, drawingRect.Top, drawingRect.Left + (cornerRadius * 2), drawingRect.Top + (cornerRadius * 2));
                    canvas.DrawArc(topLeftArcBound, 180, 90, false, paintleft);
                }
            }
        }
    }
}