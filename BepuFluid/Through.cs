using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.Entities.Prefabs;
using BEPUutilities;

namespace BepuFluid
{
    class Through
    {
        public Box Box1 { get; private set; }
        public Box Box2 { get; private set; }

        public Through(Vector3 center, float width, float heigth, float angleX, float angleZ)
        {
            Vector3 pos1 = new Vector3(center.X - width / 2, center.Y, center.Z);
            Box1 = new Box(pos1, width, 0.1f, heigth);
            Vector3 axis1 = new Vector3(1.1f, 0.5f, 2);
            Box1.Orientation = Quaternion.CreateFromAxisAngle(axis1, angleZ);

            Vector3 pos2 = new Vector3(center.X, center.Y, center.Z);
            Box2 = new Box(pos2, width, 0.1f, heigth);
            Vector3 axis2 = new Vector3(1.1f, -0.5f, -2);
            Box2.Orientation = Quaternion.CreateFromAxisAngle(axis2, angleZ);
        }
    }
}
