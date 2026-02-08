using OpenCvSharp;
using System.Linq;
using System;

namespace AlarmPP.Web.Services
{
    public sealed class VideoRailRectifier
    {
        public sealed class Settings
        {
            public double Canny1 { get; set; } = 60;
            public double Canny2 { get; set; } = 180;

            public int HoughThreshold { get; set; } = 120;
            public int MinLineLength { get; set; } = 140;
            public int MaxLineGap { get; set; } = 10;

            public double MaxAngleFromVerticalDeg { get; set; } = 25;

            public int RoiPad { get; set; } = 20;
            public int RoiMinWidth { get; set; } = 120; // чтобы ROI не стала слишком узкой
        }

        private readonly Settings _s;
        public Mat? LastH { get; private set; }
        public Rect LastRoi { get; private set; }

        public VideoRailRectifier(Settings s)
        {
            _s = s ?? new Settings();
        }

        public bool TryComputeHomography(Mat bgr, out Mat H, out Rect roiUsed)
        {
            H = new Mat();
            roiUsed = default;

            if (bgr.Empty()) return false;

            using var gray = new Mat();
            Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

            using var blur = new Mat();
            Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

            using var edges = new Mat();
            Cv2.Canny(blur, edges, _s.Canny1, _s.Canny2);

            var lines = Cv2.HoughLinesP(
                edges,
                1,
                Math.PI / 180,
                _s.HoughThreshold,
                minLineLength: _s.MinLineLength,
                maxLineGap: _s.MaxLineGap
            );

            if (lines == null || lines.Length < 2)
                return false;

            double maxAng = _s.MaxAngleFromVerticalDeg * Math.PI / 180.0;

            var vertical = lines
                .Select(l =>
                {
                    double dx = l.P2.X - l.P1.X;
                    double dy = l.P2.Y - l.P1.Y;
                    double ang = Math.Atan2(dy, dx);
                    double fromVert = Math.Abs(Math.Abs(ang) - Math.PI / 2);
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    return (line: l, fromVert, len);
                })
                .Where(x => x.fromVert <= maxAng)
                .OrderByDescending(x => x.len)
                .Take(6)
                .ToList();

            if (vertical.Count < 2)
                return false;

            var two = vertical
                .Select(v =>
                {
                    double mx = (v.line.P1.X + v.line.P2.X) * 0.5;
                    return (v.line, mx);
                })
                .OrderBy(x => x.mx)
                .Take(2)
                .ToArray();

            var left = two[0].line;
            var right = two[1].line;

            // ---- ROI между рельсами + pad ----
            int minX = Math.Min(
                Math.Min(left.P1.X, left.P2.X),
                Math.Min(right.P1.X, right.P2.X)
            );
            int maxX = Math.Max(
                Math.Max(left.P1.X, left.P2.X),
                Math.Max(right.P1.X, right.P2.X)
            );

            int pad = _s.RoiPad;
            int x = Math.Clamp(minX - pad, 0, bgr.Width - 1);
            int w = Math.Clamp((maxX - minX) + 2 * pad, 1, bgr.Width - x);

            // если вдруг рельсы близко/детект кривой — не даём ROI быть слишком узкой
            if (w < _s.RoiMinWidth)
            {
                int cx = (minX + maxX) / 2;
                w = Math.Min(_s.RoiMinWidth, bgr.Width);
                x = Math.Clamp(cx - w / 2, 0, bgr.Width - w);
            }

            // по высоте — весь кадр (можно улучшить потом)
            roiUsed = new Rect(x, 0, w, bgr.Height);

            // ---- угол поворота по средней линии ----
            double dxm = (left.P2.X - left.P1.X + right.P2.X - right.P1.X) * 0.5;
            double dym = (left.P2.Y - left.P1.Y + right.P2.Y - right.P1.Y) * 0.5;
            double angMid = Math.Atan2(dym, dxm);

            double rot = (Math.PI / 2) - angMid;
            double rotDeg = rot * 180.0 / Math.PI;

            // ВАЖНО: матрица поворота СЧИТАЕТСЯ В КООРДИНАТАХ ROI (не всего кадра)
            var roiCenter = new Point2f(roiUsed.Width / 2f, roiUsed.Height / 2f);
            using var M = Cv2.GetRotationMatrix2D(roiCenter, rotDeg, 1.0);

            H = Mat.Eye(3, 3, MatType.CV_64FC1);
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 3; c++)
                    H.Set(r, c, M.Get<double>(r, c));

            LastH?.Dispose();
            LastH = H.Clone();
            LastRoi = roiUsed;

            return true;
        }
    }
}
