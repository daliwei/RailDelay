using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;

namespace RADFE
{
    public static class Visualizer
    {
        public static RAnetwork copy_RAN = new RAnetwork();

        public static List<GraphvizColor> colors = new List<GraphvizColor>();

        public static void Visualize<TVertex, TEdge>(this IVertexAndEdgeListGraph<TVertex, TEdge> graph,
            string fileName, string dir = @"c:\temp\")
            where TEdge : IEdge<TVertex>
        {
            Visualize(graph, fileName, NoOpEdgeFormatter, dir);
        }        
        
        public static void Visualize<TVertex, TEdge>(this IVertexAndEdgeListGraph<TVertex, TEdge> graph,
            string fileName, FormatEdgeAction<TVertex, TEdge> edgeFormatter, string dir = @"c:\temp\")
            where TEdge : IEdge<TVertex>
        {
            var fullFileName = Path.Combine(dir, fileName);
            var viz = new GraphvizAlgorithm<TVertex, TEdge>(graph);

            viz.FormatVertex += VizFormatVertex;

            viz.FormatEdge += edgeFormatter;

            viz.Generate(new FileDotEngine(), fullFileName);
        }
        
        static void NoOpEdgeFormatter<TVertex, TEdge>(object sender, FormatEdgeEventArgs<TVertex, TEdge> e)
            where TEdge : IEdge<TVertex>
        {
            e.EdgeFormatter.Label.Value = LabelEdge(e.Edge.ToString());
        }
        
        public static string ToDotNotation<TVertex, TEdge>(this IVertexAndEdgeListGraph<TVertex, TEdge> graph)
            where TEdge : IEdge<TVertex>
        {
            var viz = new GraphvizAlgorithm<TVertex, TEdge>(graph);
            
            viz.FormatVertex += VizFormatVertex;
            return viz.Generate(new DotPrinter(), "");
        }

        static void VizFormatVertex<TVertex>(object sender, FormatVertexEventArgs<TVertex> e)
        {
            e.VertexFormatter.Label = LableVet(e.Vertex.ToString());

            e.VertexFormatter.Style = GraphvizVertexStyle.Filled;
            e.VertexFormatter.FillColor = ColorVet(e.Vertex.ToString());
        }

        private static string LabelEdge(string name)
        {
            return System.Convert.ToString(copy_RAN.hashtable[name]);
        }

        private static GraphvizColor ColorVet(string p)
        {
            EventNode temp = Visualizer.copy_RAN.eventlines[System.Convert.ToInt32(p)];
            return colors[temp.lineno-1];
        }

        private static string LableVet(string p)
        {
            EventNode temp = Visualizer.copy_RAN.eventlines[System.Convert.ToInt32(p)];
            if (temp.arrival == true)
            {
                return ("Arrival_" + copy_RAN.linename[temp.lineno-1] + "_St_" + temp.stationid.ToString()
                    + "_Cycle_" + temp.cycleNo);
            }
            else
            {
                return ("Departure_" + copy_RAN.linename[temp.lineno - 1] + "_St_" + temp.stationid.ToString()
                    + "_Cycle_" + temp.cycleNo);
            }
        }
    }
    public sealed class FileDotEngine : IDotEngine
    {
        public string Run(GraphvizImageType imageType, string dot, string outputFileName)
        {
            string output = outputFileName;
            File.WriteAllText(output, dot);            

            // assumes dot.exe is on the path:
            var args = string.Format(@"{0} -Tjpg -O", output);
            if (outputFileName.IndexOf("global") < 0)
            {
                System.Diagnostics.Process.Start("C:\\Program Files (x86)\\Graphviz2.38\\bin\\dot.exe", args);
                return output;
            }
            else
            {
                System.Diagnostics.Process.Start("C:\\Program Files (x86)\\Graphviz2.38\\bin\\circo.exe", args);
                return output;
            }
        }
    }
        
    public sealed class DotPrinter : IDotEngine
    {
        public string Run(GraphvizImageType imageType, string dot, string outputFileName)
        {
            return dot;
        }
    }
}
