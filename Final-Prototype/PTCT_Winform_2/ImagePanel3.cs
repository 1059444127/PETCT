﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageUtils;

namespace PTCT_Winform_2
{
    public partial class ImagePanel3 : Control
    {
        private Bitmap m_storedBitmap;
        private Bitmap m_pet_storedBitmap;

        private CoordinateMapping m_coordinateMapping;

        public ImagePanel3()
        {
            InitializeComponent();
            currentDisplayOffsetPt = new Point(0, 0);
            originalDisplayOffsetPt = new Point(0, 0);
            PetPosition = new PointF(0, 0);
            BorderSize = 2;
            currentZoomFactor = 1.0F;
            originalZoomFactor = 1.0F;
            lastMousePosition = new Point(0, 0);

            // initial coordinate system mapping
            this.m_coordinateMapping = new CoordinateMapping();
            
            
            // Set few control option.
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.UserMouse, true);

            // handle resize
            this.Resize += new EventHandler(ImagePanel3_Resize);
            
        }

        public ImagePanel3(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        private void ImagePanel3_Resize(object sender, EventArgs e)
        {
            InitializeZoomAndPosition();
        }

        public Bitmap StoreBitmap
        {
            set
            {
                if (m_storedBitmap == null)
                {
                    m_storedBitmap = value;
                    InitializeZoomAndPosition();
                }
                m_storedBitmap = value;
                Invalidate();
            }
            get { return m_storedBitmap; }
        }
        public Bitmap PET_StoreBitmap
        {
            set
            {
                if (m_pet_storedBitmap == null)
                {
                    m_pet_storedBitmap = value;
                    //InitializeZoomAndPosition();
                }
                m_pet_storedBitmap = value;
                Invalidate();
            }
            get { return m_pet_storedBitmap; }
        }
        private void InitializeZoomAndPosition()
        {
            if (StoreBitmap == null) return;

            float zoom = 1.0F;
            Point p = new Point(0, 0);
            Size effectivePanelSize = this.Size - new Size((10 + (int)(2 * this.BorderSize)), (10 + (int)(2 * this.BorderSize)));
            float diffHeight = ((float)effectivePanelSize.Height) / ((float)StoreBitmap.Height);
            float diffWidth = ((float)effectivePanelSize.Width) / ((float)StoreBitmap.Width);
            zoom = Math.Min(diffHeight, diffWidth);

            int w = (int)(StoreBitmap.Width * zoom);
            int h = (int)(StoreBitmap.Height * zoom);
            p.X = (int)((effectivePanelSize.Width - w) / (2 * zoom));
            p.X += 5 + (int)BorderSize;
            p.Y = (int)((effectivePanelSize.Height - h) / (2 * zoom));
            p.Y += 5 + (int)BorderSize;
            this.currentDisplayOffsetPt = p;
            this.originalDisplayOffsetPt = p;
            this.currentZoomFactor = zoom;
            this.originalZoomFactor = zoom;
            imageRect = new RectangleF((currentZoomFactor * currentDisplayOffsetPt.X) - 1,
                    (currentZoomFactor * currentDisplayOffsetPt.Y) - 1,
                    (currentZoomFactor * StoreBitmap.Width) + 2,
                    (currentZoomFactor * StoreBitmap.Height) + 2);


        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (StoreBitmap == null) return;

            RectangleF srcRect = new RectangleF((e.ClipRectangle.X / currentZoomFactor) - currentDisplayOffsetPt.X,
                    (e.ClipRectangle.Y / currentZoomFactor) - currentDisplayOffsetPt.Y,
                    e.ClipRectangle.Width / currentZoomFactor, e.ClipRectangle.Height / currentZoomFactor);

            Rectangle roundedRectangle = Rectangle.Round(imageRect);
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(255, 0, 0)), roundedRectangle);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            e.Graphics.DrawImage(StoreBitmap, e.ClipRectangle, srcRect, GraphicsUnit.Pixel);

            if (m_pet_storedBitmap != null)
            {
                ///step1: find the center of CT Image rect
                Point center_point_ct = new Point(roundedRectangle.Left + roundedRectangle.Width / 2,
                    roundedRectangle.Top + roundedRectangle.Height / 2);

                ///step2: calculate PT image display rect 
                PointF ptDisplayPosition1 = new PointF( roundedRectangle.Left- PetPosition.X, roundedRectangle.Top-PetPosition.Y);
             
                Rectangle pt_rect= new Rectangle( (int)(ptDisplayPosition1.X),  (int)(ptDisplayPosition1.Y),  (int)(m_pet_storedBitmap.Width*currentZoomFactor),  (int)(m_pet_storedBitmap.Height*currentZoomFactor));

                /// find center point of PT image rect
                Point center_point_pt = new Point(pt_rect.Left + pt_rect.Width / 2,
                    pt_rect.Top + pt_rect.Height / 2);

                // step3: map center of CT and PT rect;
                // CT rect is fixed; PT rect is movable.
                Point displacement = new Point(center_point_ct.X - center_point_pt.X,center_point_ct.Y - center_point_pt.Y);

                /// step4: displace pt_rect
                pt_rect.Offset(displacement);

                e.Graphics.DrawRectangle(new Pen(Color.Blue), pt_rect);
                e.Graphics.DrawImage(this.m_pet_storedBitmap, pt_rect);
            }

            Pen _pen1 = new Pen(Color.LightGoldenrodYellow, 2.0F);
            Font _font = new Font("Verdana", 10.0F);
            StringBuilder _sb = new StringBuilder();
            _sb.Append("Zoom#");
            _sb.Append(currentZoomFactor);
            e.Graphics.DrawString(_sb.ToString(), _font, _pen1.Brush, new PointF(10, 0));
            base.OnPaint(e);
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            this.lastMousePosition = new Point(e.X, e.Y);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if(e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                PointF relOffset = new PointF(lastMousePosition.X - e.X, lastMousePosition.Y - e.Y);
                PetPosition = new PointF(PetPosition.X - (int)(relOffset.X / currentZoomFactor), PetPosition.Y - (int)(relOffset.Y / currentZoomFactor));
                
                Invalidate();
            }
            this.lastMousePosition = new Point(e.X, e.Y);
        }

        public PointF GetActualDisplayPosition(PointF point)
        {
            PointF p = this.m_coordinateMapping.GetActualDisplayPosition(point);
            p = new PointF((float)(this.currentZoomFactor * (p.X + this.currentDisplayOffsetPt.X)),
                (float)(this.currentZoomFactor * (p.Y + this.currentDisplayOffsetPt.Y)));
            return p;
        }

        public Point GetOriginalCoords(Point p)
        {
            Point ret = new Point((int)((p.X / currentZoomFactor) - currentDisplayOffsetPt.X),
                (int)((p.Y / currentZoomFactor) - currentDisplayOffsetPt.Y));
            return this.m_coordinateMapping.GetActualPosition(ret);
        }

        public Point currentDisplayOffsetPt { get; set; }

        public Point originalDisplayOffsetPt { get; set; }

        public int BorderSize { get; set; }

        public float currentZoomFactor { get; set; }

        public float originalZoomFactor { get; set; }

        public Point lastMousePosition { get; set; }

        public RectangleF imageRect { get; set; }

        public PointF PetPosition { get; set; }
    }
}
