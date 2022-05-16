using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Adornment
{
    internal class ErrorsAboveBelowAdornment
    {
        private readonly IAdornmentLayer _adornmentLayer;
        private FrameworkElement _adornment;
        private readonly double _initOpacity;
        private double _currentOpacity;

        private readonly IWpfTextView _view;
        private readonly bool _isTop;

        public ErrorsAboveBelowAdornment(IWpfTextView view, double initOpacity, bool isTop, string message)
        {
            _adornmentLayer = view.GetAdornmentLayer(ErrorLayer.LayerName);
            _currentOpacity = initOpacity;
            _initOpacity = initOpacity;
            _view = view;
            _isTop = isTop;

            _adornment = CreateWPF(message);

            _view.ViewportHeightChanged += OnViewportSizeChanged;
            _view.ViewportWidthChanged += OnViewportSizeChanged;
            _view.LayoutChanged += _view_LayoutChanged;

            //if (_adornmentLayer.IsEmpty)
            _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, _adornment, null);

            view.Caret.PositionChanged += OnCaretPositionChanged;

            SetAdornmentLocation(_view);
        }

        private void _view_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            RefreshText();
        }
        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            RefreshText();
        }

        private void RefreshText()
        {
            var bufferPosition = _view.Caret.Position.BufferPosition;

            var lineCount = _view.TextSnapshot.LineCount;
            int currentLine = _view.VisualSnapshot.GetLineFromPosition(bufferPosition.Position).LineNumber;


            var firstVisibleLine = _view.TextSnapshot.GetLineNumberFromPosition(_view.TextViewLines.FirstVisibleLine.Start) + 1;
            var lastVisibleLine = _view.TextSnapshot.GetLineNumberFromPosition(_view.TextViewLines.LastVisibleLine.Start) + 1;

            var text = $"Lines: {lineCount}; Current: {currentLine}; Viewport: [{firstVisibleLine}, {lastVisibleLine}]";

            RunOnUIThread.Run(() => UpdateText(text));
        }

        private void UpdateText(string message)
        {
            var textBlock = ((Border)_adornment).Child as TextBlock;
            textBlock.Text = message;
        }

        private FrameworkElement CreateWPF(string text)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(4, 0, 4, 0)
            };

            var border = new Border             
            {
                Margin = new Thickness(3, 0, 3, 0), // Space between this UI element and the editor text
                Padding = new Thickness(0),  // Space between the side of the control and its content    
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(1),
                Child = textBlock
            };

                                                                                                                                                                                                                                                                            
            Update(border, _view.FormattedLineSource);
            border.Opacity = _initOpacity;

            //// If we don't call Measure here the tag is positioned incorrectly
            //border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            border.MouseEnter += (s, e) => { _adornment.Opacity = 1D; };
            border.MouseLeave += (s, e) => { _adornment.Opacity = _currentOpacity; };

            return border;
        }

        private void OnViewportSizeChanged(object sender, EventArgs e) => SetAdornmentLocation(_view);

        private void SetAdornmentLocation(IWpfTextView view)
        {
            if (double.IsNaN(_adornment.Width))
            {
                _adornment.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            if (double.IsNaN(_adornment.Width))
            {
                _adornment.Width = 300;
            }
            if (double.IsNaN(_adornment.Height))
            {
                _adornment.Height = _view.LineHeight;
            }

            Canvas.SetLeft(_adornment, view.ViewportRight - _adornment.Width - 10);

            if (_isTop)
            {
//                Canvas.SetTop(_adornment, view.ViewportTop - _adornment.Height - 10);
                Canvas.SetTop(_adornment, view.ViewportTop);
            }
            else
            {
                //                Canvas.SetBottom(_adornment, view.ViewportBottom - _adornment.Height - 10);
                //                Canvas.SetBottom(_adornment, view.ViewportBottom - 20 );
                Canvas.SetTop(_adornment, view.ViewportBottom - _adornment.Height - 10);
            }
        }

        /// <summary>
        /// Updates the adornment's color and font to match the VS theme.
        /// </summary>
        /// <remarks>
        /// <see cref="formattedLineSource"/> can be null if the adornment is created when the view is initialized.
        /// If it is null, we will not apply VS theme colors and instead will rely on VS to call us again
        /// when the view is initialized and <see cref="formattedLineSource"/> is no longer null.
        /// </remarks>
        private void Update(Border border, IFormattedLineSource formattedLineSource)
        {
            if (formattedLineSource == null)
            {
                return;
            }

            var textBlock = border.Child as TextBlock;

            textBlock.Foreground = formattedLineSource.DefaultTextProperties.ForegroundBrush;
            textBlock.FontSize = formattedLineSource.DefaultTextProperties.FontRenderingEmSize - 2;
            textBlock.FontFamily = formattedLineSource.DefaultTextProperties.Typeface.FontFamily;

            var themeColors = ThemeColors.BasedOnText(textBlock.Foreground);
            border.Background = themeColors.BackgroundBrush;

        }
    }
}
