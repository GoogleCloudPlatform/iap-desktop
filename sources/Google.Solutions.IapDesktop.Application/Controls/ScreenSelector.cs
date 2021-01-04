using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace Google.Solutions.IapDesktop.Application.Controls
{
    public partial class ScreenSelector<TModelItem> : UserControl
        where TModelItem : IScreenSelectorModelItem
    {
        private Point currentMouseLocation = new Point(0, 0);

        private ObservableCollection<TModelItem> model = null;

        public ScreenSelector()
        {
            InitializeComponent();

            this.ResizeRedraw = true;
            this.DoubleBuffered = true;
        }

        //---------------------------------------------------------------------
        // ScreenIcon.
        //---------------------------------------------------------------------

        internal class ScreenIcon
        {
            public TModelItem Model { get; }
            public Rectangle Bounds { get; }

            public ScreenIcon(
                TModelItem screen,
                Rectangle bounds)
            {
                this.Model = screen;
                this.Bounds = bounds;
            }
        }

        private IList<ScreenIcon> GetModelIcons()
        {
            // Calulate a bounding box around all screens.
            var unionOfAllScreens = new Rectangle();
            foreach (Screen s in this.model.Select(m => m.Screen))
            {
                unionOfAllScreens = Rectangle.Union(unionOfAllScreens, s.Bounds);
            }

            var scalingFactor = Math.Min(
                (double)this.Width / unionOfAllScreens.Width,
                (double)this.Height / unionOfAllScreens.Height);

            return this.model
                .OrderBy(modelItem => modelItem.Screen.DeviceName)

                // Shift bounds so that they have positive coordinates.
                .Select(modelItem =>
                    new ScreenIcon(
                        modelItem,
                        new Rectangle(
                            modelItem.Screen.Bounds.X + Math.Abs(unionOfAllScreens.X),
                            modelItem.Screen.Bounds.Y + Math.Abs(unionOfAllScreens.Y),
                            modelItem.Screen.Bounds.Width,
                            modelItem.Screen.Bounds.Height)))

                // Scale down to size of control.
                .Select(icon =>
                    new ScreenIcon(
                        icon.Model,
                        new Rectangle(
                            (int)((double)icon.Bounds.X * scalingFactor),
                            (int)((double)icon.Bounds.Y * scalingFactor),
                            (int)((double)icon.Bounds.Width * scalingFactor),
                            (int)((double)icon.Bounds.Height * scalingFactor))))

                // Add some padding
                .Select(icon =>
                    new ScreenIcon(
                        icon.Model,
                        new Rectangle(
                            icon.Bounds.X + 2,
                            icon.Bounds.Y + 2,
                            icon.Bounds.Width - 4,
                            icon.Bounds.Height - 4)))
                .ToList();
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void ScreenSelector_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.Black, 2))
            {
                var screenOrdinal = 1;
                foreach (var screenIcon in GetModelIcons())
                {
                    e.Graphics.FillRectangle(
                        screenIcon.Model.IsSelected 
                            ? SystemBrushes.Highlight
                            : SystemBrushes.ControlLight,
                        screenIcon.Bounds);
                    e.Graphics.DrawRectangle(
                        pen,
                        screenIcon.Bounds);
                    e.Graphics.DrawString(
                        (screenOrdinal++).ToString(), 
                        this.Font,
                        screenIcon.Model.IsSelected
                            ? Brushes.White
                            : SystemBrushes.ControlText, 
                        screenIcon.Bounds,
                        new StringFormat
                        {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Center
                        });
                }
            }
        }

        private void ScreenSelector_Click(object sender, EventArgs e)
        {
            var selected = GetModelIcons().FirstOrDefault(
                s => s.Bounds.Contains(this.currentMouseLocation));

            if (selected != null)
            {
                // Toggle selected state.
                selected.Model.IsSelected = !selected.Model.IsSelected;
                Invalidate();
            }
        }

        private void ScreenSelector_MouseMove(object sender, MouseEventArgs e)
        {
            this.currentMouseLocation = e.Location;
        }

        //---------------------------------------------------------------------
        // List Binding.
        //---------------------------------------------------------------------

        public void BindCollection(ObservableCollection<TModelItem> model)
        {
            this.model = model;
        }
    }

    public interface IScreenSelectorModelItem
    {
        Screen Screen { get; }
        bool IsSelected { get; set; }
    }
}
