using System.Collections.Generic;
using QuestPDF.Drawing.DrawingCanvases;
using QuestPDF.Elements;
using QuestPDF.Elements.Text;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Image = QuestPDF.Elements.Image;
using SvgImage = QuestPDF.Elements.SvgImage;

namespace QuestPDF.Drawing.Proxy;

internal sealed class LayoutProxy : ElementProxy
{
   
    public LayoutProxy(Element child)
    {
        Child = child;
    }
    
    internal override void Draw(Size availableSpace)
    {
        var size = ProvideIntrinsicSize() ? Child.Measure(availableSpace) : availableSpace;
        
        base.Draw(availableSpace);

        if (!Canvas.Is<SkiaDrawingCanvas>())
            return;
        
        var matrix = Canvas.GetCurrentMatrix();
        

        bool ProvideIntrinsicSize()
        {
            // Image or DynamicImage or SvgImage or DynamicSvgImage should be excluded
            // They rely on the AspectRation component to provide true intrinsic size
            
            return Child is TextBlock or AspectRatio or Unconstrained or SemanticTag or ArtifactTag;
        }
    }

    internal void CaptureLayoutErrorMeasurement()
    {
        var child = Child;
        
        while (true)
        {
            if (child is OverflowDebuggingProxy overflowDebuggingProxy)
            {
                if (overflowDebuggingProxy.AvailableSpace == null || overflowDebuggingProxy.SpacePlan == null)
                    break;
                
                
            }

            if (child is not ElementProxy proxy)
                break;
            
            child = proxy.Child;
        }
    }
}