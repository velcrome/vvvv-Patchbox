#region usings
using System;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "IOBoxGenerator", Category = "String", Help = "Basic template with one string in/out", Tags = "")]
	#endregion PluginInfo
	public class StringIOBoxGeneratorNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultString = "Input goes here")]
		ISpread<string> FInput;
		
		[Input("Prefix", DefaultString = "")]
		ISpread<string> FPrefix;
		
		[Input("Width Multiplier", DefaultValue = 500)]
		ISpread<int> FWidth;
		
		[Input("Height Multiplier", DefaultValue = 300)]
		ISpread<int> FHeight;
		
		[Input("Show Grid", DefaultValue = 0.0)]
		ISpread<bool> FShowGrid;			

		[Input("Bounds", DefaultString = "Bounds Input goes here")]
		ISpread<string> FBoundsIn;

		[Output("Match")]
		ISpread<bool> FMatch;

		[Output("Category")]
		ISpread<string> FCategory;

		[Output("Rows")]
		ISpread<int> FRows;

		[Output("Columns")]
		ISpread<int> FColumns;
		[Output("Node")]
		ISpread<string> FNode;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins
		Regex r;
		const string pattern = " *(?'Category'[ivdbcts]) *(?'Rows'\\d+)(x(?'Columns'\\d+))?";

		const string nodeTemplate = "<PATCH><NODE id=\"{id}\" createme=\"pronto\" nodename=\"{nodename}\" systemname=\"{nodename}\" componentmode=\"InABox\">{bounds}{pins}</NODE></PATCH>";
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FMatch.SliceCount = FCategory.SliceCount = FRows.SliceCount = FColumns.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++) {
				Match m = Regex.Match(FInput[i], FPrefix[i] + " *"+pattern);
				if (m.Groups["Category"].Success) {
					FCategory[i] = m.Groups["Category"].Value;
				} else {
					FCategory[i] = "No Valid Category";
				}
				if (m.Groups["Rows"].Success) {
					FRows[i] = int.Parse(m.Groups["Rows"].Value);
				} else {
					FRows[i] = 0;
				}
				if (m.Groups["Columns"].Success) {
					FColumns[i] = int.Parse(m.Groups["Columns"].Value);
				} else {
					if (m.Success)
						FColumns[i] = 1;
					else
						FColumns[i] = 0;

				}
				if (m.Success) {
					string nodename;
					if (m.Groups["Category"].Value[0] == 'c') {
						nodename = "IOBox (Color)";
					} else if (m.Groups["Category"].Value[0] == 's') {
						nodename = "IOBox (String)";
					} else {
						nodename = "IOBox (Value Advanced)";
					}
					string nodeOutput = nodeTemplate;
					nodeOutput = Regex.Replace(nodeOutput, "\\{nodename\\}", nodename);
					string pins = "";
					//ColsRows
					if ((FColumns[i] > 1) || (FRows[i] > 1))
						pins += "<PIN pinname=\"SliceCount Mode\" slicecount=\"1\" values=\"ColsRowsPages\"></PIN>";
					pins += "<PIN pinname=\"Rows\" slicecount=\"1\" values=\"" + m.Groups["Rows"].Value + "\"></PIN>";
					string cols;
					if (m.Groups["Columns"].Success)
						cols = m.Groups["Columns"].Value;
					else
						cols = "1";
					pins += "<PIN pinname=\"Columns\" slicecount=\"1\" values=\"" + cols + "\"></PIN>";

					//Value Type and Behaviour
					switch (m.Groups["Category"].Value[0]) {
						case 'i':
							//integer
							pins += "<PIN pinname=\"Value Type\" slicecount=\"1\" values=\"Integer\"></PIN>";
							break;
						case 'b':
							pins += "<PIN pinname=\"Value Type\" slicecount=\"1\" values=\"Boolean\"></PIN>";
							pins += "<PIN pinname=\"Behaviour\" slicecount=\"1\" values=\"Bang\"></PIN>";
							break;
						case 't':
							pins += "<PIN pinname=\"Value Type\" slicecount=\"1\" values=\"Boolean\"></PIN>";
							pins += "<PIN pinname=\"Behaviour\" slicecount=\"1\" values=\"Toggle\"></PIN>";
							break;
					}
					
					//Show Grid?
					string showGrid;
					if(FShowGrid[i]) showGrid = "1";
					else showGrid = "0";
					pins += "<PIN pinname=\"Show Grid\" slicecount=\"1\" values=\"" + showGrid + "\"></PIN>";
					nodeOutput = Regex.Replace(nodeOutput, "\\{pins\\}", pins);

					//Bounds
					Match boundsmatches = Regex.Match(FBoundsIn[i], "type=\"Node\" left=\"(?'left'\\d+)\" top=\"(?'top'\\d+)");
					string bounds = "";

					string left;
					if (boundsmatches.Groups["left"].Success)
						left = boundsmatches.Groups["left"].Value;
					else
						left = "1000";

					string top;
					if (boundsmatches.Groups["top"].Success)
						top = boundsmatches.Groups["top"].Value;
					else
						top = "1000";

					string width = (FColumns[i] * FWidth[i]).ToString();
					string height = (FRows[i] * FHeight[i]).ToString();

					bounds += "<BOUNDS type=\"Node\" left=\"" + left + "\" top=\"" + top + "\" width=\"" + width + "\" height=\"" + height + "\"></BOUNDS>";

					bounds += "<BOUNDS type=\"Box\" left=\"" + left + "\" top=\"" + top + "\" width=\"" + width + "\" height=\"" + height + "\"></BOUNDS>";
					nodeOutput = Regex.Replace(nodeOutput, "\\{bounds\\}", bounds);
					FNode[i] = nodeOutput;
				} else
					FNode[i] = "No match";
				FMatch[i] = m.Success;

			}

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
