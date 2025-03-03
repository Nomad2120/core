using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Components
{
    public class RadzenTextBox2 : RadzenTextBox
    {
        [Parameter]
        public bool ChangeOnInput { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            base.BuildRenderTree(__builder);
            if (ChangeOnInput)
            {
#pragma warning disable BL0006 // Do not use RenderTree types
                var frames = __builder.GetFrames().Clone();
                __builder.Clear();
                int sequenceAdd = 0;
                foreach (var frame in frames.Array)
                {
                    switch (frame.FrameType)
                    {
                        case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Element:
                            __builder.OpenElement(frame.Sequence + sequenceAdd, frame.ElementName);
                            break;
                        case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Text:
                            __builder.AddContent(frame.Sequence + sequenceAdd, frame.TextContent);
                            break;
                        case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Attribute:
                            __builder.AddAttribute(frame.Sequence + sequenceAdd, frame);
                            if (frame.AttributeName == "onchange")
                            {
                                __builder.AddAttribute(frame.Sequence + ++sequenceAdd, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, OnChange));
                            }
                            break;
                        //case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Component:
                        //    __builder.OpenComponent(frame.Sequence + sequenceAdd, frame.Component.GetType());
                        //    break;
                        //case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Region:
                        //    __builder.OpenRegion(frame.Sequence + sequenceAdd);
                        //    break;
                        case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.ElementReferenceCapture:
                            __builder.AddElementReferenceCapture(frame.Sequence + sequenceAdd, frame.ElementReferenceCaptureAction);
                            break;
                        //case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.ComponentReferenceCapture:
                        //    __builder.AddComponentReferenceCapture(frame.Sequence + sequenceAdd, frame.ComponentReferenceCaptureAction);
                        //    break;
                        case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Markup:
                            __builder.CloseElement();
                            __builder.AddMarkupContent(frame.Sequence + sequenceAdd, frame.MarkupContent);
                            break;
                        case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.None:
                        default:
                            break;
                    }
                }
#pragma warning restore BL0006 // Do not use RenderTree types
            }
        }

        public new Task OnChange(ChangeEventArgs args)
        {
            return base.OnChange(args);
        }
    }
}
