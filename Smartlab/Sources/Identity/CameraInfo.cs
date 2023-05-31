using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMU.Smartlab.Identity
{
    public class CameraInfo
    {
        Point3D Location;
        Point3D x;
        Point3D y;
        Point3D z;
        private Point3D InverseX;
        private Point3D InverseY;
        private Point3D InverseZ;

        public CameraInfo(Point3D location, Point3D dir_x = null, Point3D dir_y = null, Point3D dir_z = null)
        {
            this.Location = location;
            int num_null = 0;
            if (dir_x is null)
            {
                num_null += 1;
            }
            if (dir_y is null)
            {
                num_null += 1;
            }
            if (dir_z is null)
            {
                num_null += 1;
            }
            if (num_null > 1)
            {
                throw new ArgumentNullException("At least two axes should be provided");
            }
            if (!(dir_x is null) && !(dir_y is null))
            {
                this.x = dir_x.Normalize();
                this.y = dir_y.Normalize();
                this.z = PUtil.Cross(this.x, this.y);
            }
            else if (!(dir_y is null) && !(dir_z is null))
            {
                this.y = dir_y.Normalize();
                this.z = dir_z.Normalize();
                this.x = PUtil.Cross(this.y, this.z);
            }
            else
            {
                this.z = dir_z.Normalize();
                this.x = dir_x.Normalize();
                this.y = PUtil.Cross(this.z, this.x);
            }
            InverseAxes();
        }

        private void InverseAxes()
        {
            double norm_factor = x.x * y.y * z.z +
                x.y * y.z * z.x +
                x.z * y.x * z.y -
                x.z * y.y * z.x -
                x.y * y.x * z.z -
                x.x * y.z * z.y;
            this.InverseX = new Point3D(y.y * z.z - y.z * z.y, x.z * z.y - x.y * z.z, x.y * y.z - x.z * y.y) / norm_factor;
            this.InverseY = new Point3D(y.z * z.x - y.x * z.z, x.x * z.z - x.z * z.x, x.z * y.x - x.x * y.z) / norm_factor;
            this.InverseZ = new Point3D(y.x * z.y - y.y * z.x, x.y * z.x - x.x * z.y, x.x * y.y - x.y * y.x) / norm_factor;
        }

        public Point3D Cam2World(Point3D coord)
        {
            return coord.x * this.x + coord.y * this.y + coord.z * this.z + this.Location;
        }

        public Point3D World2Cam(Point3D coord)
        {
            Point3D temp = coord - this.Location;
            return temp.x * this.InverseX + temp.y * this.InverseY + temp.z * this.InverseZ;
        }
    }
}
