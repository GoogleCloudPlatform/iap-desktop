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
    public partial class ScreenSelector : UserControl
    {
        private IList<ScreenIcon> screens;
        private Point currentMouseLocation = new Point(0, 0);

        private ObservableCollection<string> selectedScreens = null;

        internal class ScreenIcon
        {
            public Screen Screen { get; }
            public Rectangle Bounds { get; }

            public ScreenIcon(
                Screen screen,
                Rectangle bounds)
            {
                this.Screen = screen;
                this.Bounds = bounds;
            }
        }

        public ScreenSelector()
        {
            InitializeComponent();

            this.ResizeRedraw = true;
            this.DoubleBuffered = true;
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void ScreenSelector_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.Black, 2))
            {
                var screenOrdinal = 1;
                foreach (var screen in this.screens)
                {
                    var active = this.selectedScreens != null &&
                        this.selectedScreens.Contains(screen.Screen.DeviceName);

                    e.Graphics.FillRectangle(
                        active 
                            ? SystemBrushes.Highlight
                            : SystemBrushes.ControlLight,
                        screen.Bounds);
                    e.Graphics.DrawRectangle(
                        pen,
                        screen.Bounds);
                    e.Graphics.DrawString(
                        (screenOrdinal++).ToString(), 
                        this.Font, 
                        active 
                            ? Brushes.White
                            : SystemBrushes.ControlText, 
                        screen.Bounds,
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
            var selected = this.screens.FirstOrDefault(s => s.Bounds.Contains(this.currentMouseLocation));
            if (selected != null && this.selectedScreens != null)
            {
                if (!this.selectedScreens.Remove(selected.Screen.DeviceName))
                {
                    this.selectedScreens.Add(selected.Screen.DeviceName);
                }

                Invalidate();
            }
        }

        private void ScreenSelector_Resize(object sender, EventArgs e)
        {
            // Calulate a bounding box around all screens.
            var allScreens = Screen.AllScreens;
            var unionOfAllScreens = new Rectangle();
            foreach (Screen s in allScreens)
            {
                unionOfAllScreens = Rectangle.Union(unionOfAllScreens, s.Bounds);
            }

            var scalingFactor = Math.Min(
                (double)this.Width / unionOfAllScreens.Width,
                (double)this.Height / unionOfAllScreens.Height);

            this.screens = allScreens
                .OrderBy(s => s.DeviceName)
                // Shift bounds so that they have positive coordinates.
                .Select(screen =>
                    new ScreenIcon(
                        screen,
                        new Rectangle(
                            screen.Bounds.X + Math.Abs(unionOfAllScreens.X),
                            screen.Bounds.Y + Math.Abs(unionOfAllScreens.Y),
                            screen.Bounds.Width,
                            screen.Bounds.Height)))

                // Scale down to size of control.
                .Select(icon =>
                    new ScreenIcon(
                        icon.Screen,
                        new Rectangle(
                            (int)((double)icon.Bounds.X * scalingFactor),
                            (int)((double)icon.Bounds.Y * scalingFactor),
                            (int)((double)icon.Bounds.Width * scalingFactor),
                            (int)((double)icon.Bounds.Height * scalingFactor))))

                // Add some padding
                .Select(icon =>
                    new ScreenIcon(
                        icon.Screen,
                        new Rectangle(
                            icon.Bounds.X + 2,
                            icon.Bounds.Y + 2,
                            icon.Bounds.Width - 4,
                            icon.Bounds.Height - 4)))
                .ToList();
        }

        private void ScreenSelector_MouseMove(object sender, MouseEventArgs e)
        {
            this.currentMouseLocation = e.Location;
        }

        //---------------------------------------------------------------------
        // List Binding.
        //---------------------------------------------------------------------

        public void BindCollection(ObservableCollection<string> model)
        {
            this.selectedScreens = model;
        }
    }
}
