using System.Collections.Generic;
using UnityEngine;

namespace asim.unity.utils.geometry
{
    /// <summary>
    /// 2D Implementation of the Doubly connected edge list (DCEL) or "HalfEdge Data Structure"
    /// Best Explanation : https://jerryyin.info/geometry-processing-algorithms/half-edge/
    /// Refrence Code : https://www2.cs.sfu.ca/~binay/813.2011/DCEL.pdf
    /// In order to perform mesh/triangles/polygon operations, a better data structure is required to represent the mesh/triangles/polygon
    /// 3 classess is required for DCEL each with interrefrences to each other
    /// Vertex,
    /// Face,
    /// Halfedge
    /// 
    /// <face-list>
    /// A mesh is define by a vertices + list of indices for "the draw order" to draw in graphics card.
    /// Typically the list of indicies will be given in multiples of 3 (for Mesh Triangular faces), or 4 (for Mesh Quad Faces)
    /// This indicies is also called the "face-list"
    /// 
    /// Think of Halfedges a the normal edge that is split into two edges with direction going from and to a vertex
    /// </summary>
    public class DoublyConnectedEdgeList
    {
        public List<Vertex> Vertices = new();
        public List<Face> Faces = new();
        public List<HalfEdge> HalfEdges = new();

        /// <summary>
        /// Adding based on Clockwise winding
        /// </summary>
        public static DoublyConnectedEdgeList CreateFromTriangleMesh(List<Vector3> vertices, List<int> indices)
        {
            DoublyConnectedEdgeList dcel = new();
            foreach (Vector3 vert in vertices)
            {
                Vertex newVertex = new Vertex(vert);
                dcel.Vertices.Add(newVertex);
            }

            List<HalfEdge> edgeWithEmptyFaceList = new(); // cache list to keep track of extra halfedges that doesnt have a face

            for (int i = 0; i < indices.Count / 3; i++)
            {
                var index = i * 3;
                Vertex v0 = dcel.Vertices[indices[index + 0]];
                Vertex v1 = dcel.Vertices[indices[index + 1]];
                Vertex v2 = dcel.Vertices[indices[index + 2]];

                //Find if we have an existing halfedge that points to the correct next vertex in our list, otherwise create new
                HalfEdge he0 = FindHalfEdge(edgeWithEmptyFaceList, v0);
                if (he0 != null && he0.Next.Origin == v1)
                {
                    edgeWithEmptyFaceList.Remove(he0);
                }
                else he0 = new HalfEdge(v0);

                HalfEdge he1 = FindHalfEdge(edgeWithEmptyFaceList, v1);
                if (he1 != null && he1.Next.Origin == v2)
                {
                    edgeWithEmptyFaceList.Remove(he1);
                }
                else he1 = new HalfEdge(v1);

                HalfEdge he2 = FindHalfEdge(edgeWithEmptyFaceList, v2);
                if (he2 != null && he2.Next.Origin == v0)
                {
                    edgeWithEmptyFaceList.Remove(he2);
                }
                else he2 = new HalfEdge(v2);

                he0.Next = he1;
                he1.Next = he2;
                he2.Next = he0;

                he0.Previous = he2;
                he1.Previous = he0;
                he2.Previous = he1;

                //Create Twin if empty(null)
                if(he0.Twin == null)
                {
                    var twin = new HalfEdge(he0.Next.Origin);
                    twin.Next = he0;
                    twin.Twin = he0;

                    he0.Twin = twin;
                    edgeWithEmptyFaceList.Add(twin);
                }
                if (he1.Twin == null)
                {
                    var twin = new HalfEdge(he1.Next.Origin);
                    twin.Next = he1;
                    twin.Twin = he1;

                    he1.Twin = twin;
                    edgeWithEmptyFaceList.Add(twin);
                }
                if (he2.Twin == null)
                {
                    var twin = new HalfEdge(he2.Next.Origin);
                    twin.Next = he2;
                    twin.Twin = he2;

                    he2.Twin = twin;
                    edgeWithEmptyFaceList.Add(twin);
                }

                Face face = new Face(he0, he1, he2);

                dcel.Faces.Add(face);
                dcel.HalfEdges.Add(he0);
                dcel.HalfEdges.Add(he1);
                dcel.HalfEdges.Add(he2);
            }

            // Lastly, Connect remanining edgeWithEmptyFaceList - which will be the boundary edges halfedge twin's
            for (int i = 0; i < edgeWithEmptyFaceList.Count; i++)
            {
                var currentEdge = edgeWithEmptyFaceList[i];
                var next = FindHalfEdge(edgeWithEmptyFaceList, currentEdge.Next.Origin);
                currentEdge.Next = next;
                next.Previous = currentEdge;

                dcel.HalfEdges.Add(currentEdge);
            }

            return dcel;
        }

        /// <summary>
        /// Get the HalfEdge in the list, define by origin
        /// </summary>
        static HalfEdge FindHalfEdge(List<HalfEdge> list, Vertex origin)
        {
            foreach (var hewef in list)
            {
                if (hewef.Origin == origin) return hewef;
            }
            return null;
        }

        public class Vertex
        {
            public Vector3 Pos; // Position - xyz
            public HalfEdge IncidentEdge; // The outgoing halfedge that points to another vertex

            public Vertex(Vector3 pos)
            {
                Pos = pos;
            }
        }

        public class Face
        {
            public HalfEdge StartHalfEdge; // The starting halfedge 

            public Face(HalfEdge startHalfEdge, params HalfEdge[] otherHalfEdges)
            {
                StartHalfEdge = startHalfEdge;

                StartHalfEdge.IncidentFace = this;
                foreach (var he in otherHalfEdges)
                {
                    he.IncidentFace = this;
                }
            }
        }

        public class HalfEdge
        {
            public Vertex Origin;
            public HalfEdge Twin;
            public Face IncidentFace;
            public HalfEdge Next;
            public HalfEdge Previous;

            public HalfEdge(Vertex origin)
            {
                Origin = origin;
                Origin.IncidentEdge = this;
            }
        }

        // Common Operations 
        // https://cs418.cs.illinois.edu/website/text/halfedge.html#walk-a-face

        /// <summary>
        /// Returns the half edges around a given face
        /// </summary>
        public static IEnumerable<HalfEdge> WalkFace(Face face)
        {
            var starthe = face.StartHalfEdge;
            var currehthe = starthe;
            do
            {
                yield return currehthe;

                currehthe = currehthe.Next;

            } while (currehthe != starthe);
        }

        /// <summary>
        /// Returns the half edges around a vertex, (vertex ring / vertex umbrella)
        /// </summary>
        public static IEnumerable<HalfEdge> WalkVertex(Vertex vertex)
        {
            var starthe = vertex.IncidentEdge;
            var currehthe = starthe;
            do
            {
                yield return currehthe;

                yield return currehthe.Previous;

                currehthe = currehthe.Previous.Twin;

            } while (currehthe != starthe);
        }

        /// <summary>
        /// The Algorithm FlipEdge will consider an edge define connecting the start and end vertices shared by two triangular faces
        /// To perform a swap or flip, to use different start and end vertices
        /// Returns true if success, false if given edge is a boundary
        /// </summary>
        public static bool FlipEdge(HalfEdge e)
        {
            //Check if both side of edge has a face
            var edgeFace = e.IncidentFace;
            var twinFace = e.Twin.IncidentFace;

            if (edgeFace == null || twinFace == null) return false;

            var prev = e.Previous;
            var next = e.Next;
            var twin = e.Twin;

            // Relink first half (edge side)
            edgeFace.StartHalfEdge = prev;

            prev.Next = twin.Next;

            e.Origin = twin.Next.Next.Origin;
            twin.Next.Next = e;
            twin.Next.IncidentFace = edgeFace;

            e.Next = prev;

            //Relink second half (twin side)
            twinFace.StartHalfEdge = twin.Previous;

            twin.Previous.Next = next;

            twin.Origin = next.Next.Origin;
            next.Next = twin;
            next.IncidentFace = twinFace;

            twin.Next = twin.Previous;

            return true;
        }


        // Split Edge - split the edge of two faces, to create 4 triangles, think of subdivision (triangle mesh)

        // Refine edge - Add a Vertex and add a new adjacent edge to the face.

        // Add Edge
    }
}
