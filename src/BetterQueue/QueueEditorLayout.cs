using System;

namespace BetterQueue
{
    // Canvas units, measured from the top. The native scoreboard supplies the
    // width; it is never narrowed to make room for this editor.
    internal sealed class QueueEditorLayout
    {
        internal readonly float Width,Height,SidebarWidth,ContentLeft,ContentWidth,Cell,PlanTop,PlanHeight,PlanWidth,LoopLeft,LoopTop,LibraryHeadingTop,LibraryTop,LibraryHeight;
        internal readonly int Columns,PlanColumns;
        internal QueueEditorLayout(float width,float height,bool searchable=false)
        {
            Width=Math.Max(240,width);Height=Math.Max(320,height);
            SidebarWidth=Math.Max(84,Math.Min(160,Width*.18f));
            ContentLeft=SidebarWidth+20;ContentWidth=Width-ContentLeft-8;
            float usable=ContentWidth-8;
            Columns=Math.Max(2,(int)Math.Ceiling((usable+6)/70));
            Cell=(usable-(Columns-1)*6)/Columns;
            PlanTop=64;PlanHeight=Cell+8;
            PlanWidth=ContentWidth-Cell-6;
            PlanColumns=Math.Max(1,(int)Math.Floor((PlanWidth-8+6+.01f)/(Cell+6)));
            LoopLeft=ContentLeft+PlanWidth+6;LoopTop=PlanTop+4;
            LibraryHeadingTop=PlanTop+PlanHeight+12;
            LibraryTop=LibraryHeadingTop+(searchable?36:22);
            LibraryHeight=Height-24-LibraryTop;
        }
    }
}
