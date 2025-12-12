#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraphicsCat.MarkupShaderGUIInternal
{
    public class ConditionalTag : Tag
    {
        override public void OnBegin(MarkupShaderGUI.Context context)
        {
            var args = AttributeUtils.ExtractArgs(context.attribute);
            if (args.Count < 3)
                return;

            var comparePropName = args[0];
            var compareMode = args[1];
            var compareValue = Convert.ToSingle(args[2]);

            var compareProp = context.materialProperties.FirstOrDefault(p => p.name == comparePropName);
            if (compareProp.hasMixedValue)
            {
                state = false;
                return;
            }

            switch (compareMode)
            {
                case "Less":
                    if (compareProp.floatValue >= compareValue)
                        state = false;
                    break;
                case "Equal":
                    if (compareProp.floatValue != compareValue)
                        state = false;
                    break;
                case "LessEqual":
                    if (compareProp.floatValue > compareValue)
                        state = false;
                    break;
                case "Greater":
                    if (compareProp.floatValue <= compareValue)
                        state = false;
                    break;
                case "NotEqual":
                    if (compareProp.floatValue == compareValue)
                        state = false;
                    break;
                case "GreaterEqual":
                    if (compareProp.floatValue < compareValue)
                        state = false;
                    break;
            }
        }

        override public void OnEnd()
        {
            state = true;
        }
    }
}

#endif