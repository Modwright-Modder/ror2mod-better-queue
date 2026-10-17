using UnityEngine;
using UnityEngine.UI;

namespace BetterQueue
{
    // Original vector geometry: two circular arrows, with no font glyph or
    // borrowed mod texture. Uses the same UI color/alpha and masking as Images.
    internal sealed class LoopGlyph:MaskableGraphic
    {
        public override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect=GetPixelAdjustedRect();
            float size=Mathf.Min(rect.width,rect.height);
            Arc(mesh,rect.center,size,20,160);
            Arc(mesh,rect.center,size,200,340);
        }
        private void Arc(VertexHelper mesh,Vector2 center,float size,float start,float end)
        {
            float radius=size*.35f,halfStroke=size*.055f;
            const int segments=20;
            for(int i=0;i<segments;i++)
            {
                float a=Mathf.Lerp(start,end,i/(float)segments)*Mathf.Deg2Rad;
                float b=Mathf.Lerp(start,end,(i+1)/(float)segments)*Mathf.Deg2Rad;
                Vector2 u=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                int first=mesh.currentVertCount;
                mesh.AddVert(center+u*(radius-halfStroke),color,Vector2.zero);
                mesh.AddVert(center+u*(radius+halfStroke),color,Vector2.zero);
                mesh.AddVert(center+v*(radius+halfStroke),color,Vector2.zero);
                mesh.AddVert(center+v*(radius-halfStroke),color,Vector2.zero);
                mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);
            }
            float angle=end*Mathf.Deg2Rad;
            var outward=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            var tangent=new Vector2(-outward.y,outward.x);
            var tip=center+outward*radius;
            int index=mesh.currentVertCount;
            mesh.AddVert(tip+tangent*size*.15f,color,Vector2.zero);
            mesh.AddVert(tip-tangent*size*.08f+outward*size*.15f,color,Vector2.zero);
            mesh.AddVert(tip-tangent*size*.08f-outward*size*.15f,color,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2);
        }
    }
}
