namespace Packer
{
    public struct Area
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public Area[] Split(PackableRectangle rectangle)
        {
            var areas = new Area[]
            {
                new Area()
                {
                    X = this.X + rectangle.Width,
                    Y = this.Y,
                    Width = this.Width - rectangle.Width,
                    Height = rectangle.Height
                },
                new Area()
                {
                    X = this.X,
                    Y = this.Y + rectangle.Height,
                    Width = this.Width,
                    Height = this.Height - rectangle.Height
                }
            };
            return areas;
        }

    }
}
