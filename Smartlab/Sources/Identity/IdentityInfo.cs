using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace CMU.Smartlab.Identity
{
    public class IdentityInfo: IComparable
    {
        private static int ClusterNum = 0;

        public DateTime Timestamp 
        {
            get; set;
        }

        public String Identity
        {
            get; set;
        }
            
        public Point3D Position
        {
            get; set;
        }

        private string ClusterName;

        public string TrueIdentity
        {
            get
            {
                return this.ClusterName;
            }
            private set
            {
                this.ClusterName = value;
            }
        }

        public IdentityInfo()
        {

        }

        public IdentityInfo LastMatch;
        public IdentityInfo NextMatch;

        public IdentityInfo(DateTime timestamp, String identity, Point3D position)
        {
            this.Timestamp = timestamp;
            this.Identity = identity;
            this.Position = position;
            this.LastMatch = null;
            this.NextMatch = null;
        }

        public static IdentityInfo Parse(long timestamp, string info)
        {
            string[] raw_info = info.Split('&');
            string id = raw_info[0];
            string[] raw_pos = raw_info[1].Split(':');
            double x = double.Parse(raw_pos[0]);
            double y = double.Parse(raw_pos[1]);
            double z = double.Parse(raw_pos[2]);
            return new IdentityInfo(new DateTime(timestamp), id, new Point3D(x, y, z));
        }

        public static void MakeLink(IdentityInfo id1, IdentityInfo id2)
        {
            while (id1.NextMatch != null)
            {
                id1 = id1.NextMatch;
            }
            while (id2.LastMatch != null)
            {
                id2 = id2.LastMatch;
            }
            id1.NextMatch = id2;
            id2.LastMatch = id1;
            id2.ClusterName = id1.TrueIdentity;
        }

        public int CompareTo(object obj)
        {
            return Timestamp.CompareTo(obj);
        }

        public void Dispose()
        {
            if (this.LastMatch != null)
            {
                this.LastMatch.NextMatch = null;
                this.LastMatch = null;
            }
            if (this.NextMatch != null)
            {
                this.NextMatch.LastMatch = null;
                this.NextMatch = null;
            }
        }

        public void NewIdentity()
        {
            this.ClusterName = $"Person_{ClusterNum}";
            ClusterNum += 1;
        }

        public int SameAs(IdentityInfo another)
        {
            if (this.Timestamp.Subtract(another.Timestamp).TotalSeconds < 0.5 && PUtil.Distance(this.Position, another.Position) > 100)
            {
                return -1;
            }
            else if (this.Identity.Equals(another.Identity))
            {
                return 1;
            }
            else if (this.Timestamp.Subtract(another.Timestamp).TotalSeconds < 0.5 && PUtil.Distance(this.Position, another.Position) < 20)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        ~IdentityInfo()
        {
            this.Dispose();
        }
    }
}
