using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using MathNet.Numerics.LinearRegression;

public class CenterlineLabeling
{
    static Vector2Int[] GetLineSamples(
        bool invertXY,
        int resolution,
        int minX,
        int minY,
        int xRange,
        int yRange,
        float slope,
        int intercept,
        int interceptOffset)
    {
        Vector2Int[] result = new Vector2Int[resolution];

        if (invertXY)
        {
            for (int i = 0; i < resolution; i++)
            {
                int y = minY + (Mathf.RoundToInt(yRange * ((float)i / (float)resolution)));
                int x = (Mathf.RoundToInt(slope * y)) + intercept + interceptOffset;

                result[i] = new Vector2Int(x, y);
            }
        } else
        {
            for (int i = 0; i < resolution; i++)
            {
                int x = minX + (Mathf.RoundToInt(xRange * ((float)i / (float)resolution)));
                int y = (Mathf.RoundToInt(slope * x)) + intercept + interceptOffset;

                result[i] = new Vector2Int(x, y);
            }
        } 

        return result;
    }

    static Vector2Int[] DrawLineSamples(LineData v, bool invertXY)
    {
        Vector2Int[] result = new Vector2Int[v.resolution];

        if (invertXY)
        {
            for (int i = 0; i < v.resolution; i++)
            {
                int y = v.minY + (Mathf.RoundToInt(v.yRange * ((float)i / (float)v.resolution)));
                int x = (Mathf.RoundToInt(v.slope * y)) + v.intercept + v.offset;

                result[i] = new Vector2Int(x, y);
            }
        } else
        {
            for (int i = 0; i < v.resolution; i++)
            {
                int x = v.minX + (Mathf.RoundToInt(v.xRange * ((float)i / (float)v.resolution)));
                int y = (Mathf.RoundToInt(v.slope * x)) + v.intercept + v.offset;

                result[i] = new Vector2Int(x, y);
            }
        }

        return result;
    }

    static Vector2Int GetPointOnLine(LineData v, float t, bool invertXY)
    {
        Vector2Int result = Vector2Int.zero;

        if (invertXY)
        {
            int y = v.minY + (Mathf.RoundToInt(v.yRange * t));
            int x = (Mathf.RoundToInt(v.slope * y)) + v.intercept + v.offset;

            result = new Vector2Int(x, y);
        } else
        {
            int x = v.minX + (Mathf.RoundToInt(v.xRange * t));
            int y = (Mathf.RoundToInt(v.slope * x)) + v.intercept + v.offset;

            result = new Vector2Int(x, y);
        }

        return result;
    }

    static int GetInterceptThroughPoint(float slope, Vector2Int throughPoint, bool invertXY)
    {
        if (invertXY)
            return Mathf.RoundToInt(-(slope * (float)throughPoint.y) + throughPoint.x);
        else
            return Mathf.RoundToInt(-(slope * (float)throughPoint.x) + throughPoint.y);
    }

    static Vector2Int GetLineVector(LineData v, bool invertXY)
    {
        Vector2Int result = Vector2Int.zero;

        if (invertXY)
        {
            int y1 = v.minY + (Mathf.RoundToInt(v.yRange * 0f));
            int x1 = (Mathf.RoundToInt(v.slope * y1)) + v.intercept + v.offset;

            int y2 = v.minY + (Mathf.RoundToInt(v.yRange * 1f));
            int x2 = (Mathf.RoundToInt(v.slope * y2)) + v.intercept + v.offset;

            Vector2Int p0 = new Vector2Int(x1, y1);
            Vector2Int p1 = new Vector2Int(x2, y2);

            result = p1 - p0;
        }
        else
        {
            int x1 = v.minX + (Mathf.RoundToInt(v.xRange * 0f));
            int y1 = (Mathf.RoundToInt(v.slope * x1)) + v.intercept + v.offset;

            int x2 = v.minX + (Mathf.RoundToInt(v.xRange * 1f));
            int y2 = (Mathf.RoundToInt(v.slope * x2)) + v.intercept + v.offset;

            Vector2Int p0 = new Vector2Int(x1, y1);
            Vector2Int p1 = new Vector2Int(x2, y2);

            result = p1 - p0;
        }

        return result;
    }

    static void ImproveCenteringOfTwoPointCase(Vector2Int pA, Vector2Int pC, Texture2D provinceTex, Color32 colorId, out List<Vector2Int> newPoints, int resolution = 30)
    {
        newPoints = new List<Vector2Int>(3);

        List<Vector2Int> firstEdges = new List<Vector2Int>();
        List<Vector2Int> secondEdges = new List<Vector2Int>();
        List<float> distances = new List<float>();

        bool firstEdgeFound = false;
        bool secondEdgeFound = false;

        Vector2Int firstEdgeTemp = Vector2Int.zero;
        Vector2Int secondEdgeTemp = Vector2Int.zero;

        Color32 col;

        Vector2Int vecAC = pC - pA;
        Vector2Int vecAB = vecAC / 2;

        for (int i = 0; i < resolution; i++)
        {
            float interpolate = -1f + (2f * ((float)i / (float)resolution));
            Vector2Int newPointVec = pA + vecAB + new Vector2Int(Mathf.RoundToInt(vecAC.x * interpolate), Mathf.RoundToInt(vecAC.y * interpolate));

            col = provinceTex.GetPixel(newPointVec.x, newPointVec.y);

            if (MapTools.ColorIDEquals(colorId, col))
            {
                if (firstEdgeFound)
                {
                    secondEdgeFound = true;
                    secondEdgeTemp = newPointVec;
                }
                else
                {
                    firstEdgeFound = true;
                    firstEdgeTemp = newPointVec;
                }
            }
            else
            {
                if (firstEdgeFound)
                {
                    if (!MapTools.ColorIDEquals(col, Color.black))
                    {
                        if (secondEdgeFound)
                        {
                            //end the line drawing
                            firstEdgeFound = false;
                            secondEdgeFound = false;
                            firstEdges.Add(firstEdgeTemp);
                            secondEdges.Add(secondEdgeTemp);
                            distances.Add(Vector2.Distance(secondEdgeTemp, firstEdgeTemp));
                        }
                        else
                        {
                            //end the line drawing
                            firstEdgeFound = false;
                            firstEdges.Add(firstEdgeTemp);
                            secondEdges.Add(firstEdgeTemp);
                            distances.Add(0f);
                        }
                    }
                }
            }
        }

        //if we're done, but we were in the middle of drawing a line, finish that line
        if (firstEdgeFound)
        {
            firstEdges.Add(firstEdgeTemp);
            secondEdges.Add(secondEdgeTemp);
            distances.Add(Vector2.Distance(secondEdgeTemp, firstEdgeTemp));
        }

        int longestIndex = 0;
        float maxDistance = 0f;

        for (int i = 0; i < distances.Count; i++)
        {
            if (distances[i] > maxDistance)
            {
                longestIndex = i;
                maxDistance = distances[i];
            }
        }

        newPoints.Add(firstEdges[longestIndex]);
        newPoints.Add((secondEdges[longestIndex] + firstEdges[longestIndex]) / 2);
        newPoints.Add(secondEdges[longestIndex]);

    }

    static bool TryGetLineMidpointOverProvince(int resolution, Vector2Int[] lineSamples, Texture2D provinceTex, Color32 operativeColorID, out float distance, out Vector2Int midpoint)
    {
        Vector2Int firstEdge = Vector2Int.zero;
        Vector2Int secondEdge = Vector2Int.zero;
        bool foundFirstEdge = false;

        List<float> distances = new List<float>();
        List<Vector2Int> firstEdges = new List<Vector2Int>();
        List<Vector2Int> secondEdges = new List<Vector2Int>();

        for (int i = 0; i < resolution; i++)
        {
            //See if PixelsXInt contains the operative sampleX
            Color32 col = provinceTex.GetPixel(lineSamples[i].x, lineSamples[i].y);

            //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //go.transform.localScale /= 30f;
            //go.transform.position = new Vector3(lineSamples[i].x * (100f / 8192f), 0f, lineSamples[i].y * (100f / 8192f));

            if (MapTools.ColorIDEquals(col, operativeColorID))
            {
                if (foundFirstEdge)
                {
                    secondEdge = lineSamples[i];
                }
                else
                {
                    firstEdge = lineSamples[i];
                    foundFirstEdge = true;
                }
            }
            else
            {
                if (foundFirstEdge)
                {
                    if (secondEdge != Vector2Int.zero)
                    {
                        distances.Add(Vector2.Distance(secondEdge, firstEdge));
                        firstEdges.Add(firstEdge);
                        secondEdges.Add(secondEdge);
                        foundFirstEdge = false;
                    } 
                    else
                    {
                        distances.Add(0f);
                        firstEdges.Add(firstEdge);
                        secondEdges.Add(firstEdge);
                        foundFirstEdge = false;
                    }
                }
            }
        }

        if (distances.Count == 0 && firstEdge != Vector2Int.zero && secondEdge != Vector2Int.zero)
        {
            distances.Add(Vector2.Distance(secondEdge, firstEdge));
            firstEdges.Add(firstEdge);
            secondEdges.Add(secondEdge);
        }

        int longestIndex = 0;
        float maxDistance = 0f;

        for (int i = 0; i < distances.Count; i++)
        {
            if (distances[i] > maxDistance)
            {
                maxDistance = distances[i];
                longestIndex = i;
            }
        }

        //for (int i = 0; i < distances.Count; i++)
        //{
        //    GameObject go1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //    go1.transform.localScale /= 10f;
        //    go1.transform.position = new Vector3(firstEdges[i].x * (100f / 8192f), 0f, firstEdges[i].y * (100f / 8192f));

        //    GameObject go2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //    go2.transform.localScale /= 10f;
        //    go2.transform.position = new Vector3(secondEdges[i].x * (100f / 8192f), 0f, secondEdges[i].y * (100f / 8192f));
        //}

        if (distances.Count > 0)
        {
            distance = distances[longestIndex];
            midpoint = (firstEdges[longestIndex] + secondEdges[longestIndex]) / 2;
            //Debug.Log(firstEdges[longestIndex].ToString() + " " + secondEdges[longestIndex].ToString());

            return true;
        }
        else
        {
            distance = 0f;
            midpoint = Vector2Int.zero;

            return false;
        }
    }

    static Vector2Int GetDirectMidpointOfLine(Vector2Int[] lineSamples, out float distance)
    {
        distance = Vector2.Distance(lineSamples[0], lineSamples[lineSamples.Length - 1]);
        return (lineSamples[0] + lineSamples[lineSamples.Length - 1]) / 2;
    }

    static float GetLineThroughProvinceColorDistance(
        int resolution,
        Texture2D provinceTex,
        Vector2Int[] lineSamples,
        Color32 operativeColorID)
    {
        Vector2Int firstEdge = Vector2Int.zero;
        Vector2Int secondEdge = Vector2Int.zero;
        bool foundFirstEdge = false;

        //Raycast both lines to determine which is longer.
        //raycast horizontal
        for (int i = 0; i < resolution; i++)
        {
            //See if PixelsXInt contains the operative sampleX
            Color32 col = provinceTex.GetPixel(lineSamples[i].x, lineSamples[i].y);

            if (MapTools.ColorIDEquals(col, operativeColorID))
            {
                if (foundFirstEdge)
                {
                    secondEdge = lineSamples[i];
                }
                else
                {
                    firstEdge = lineSamples[i];
                    foundFirstEdge = true;
                }
            }
        }

        return Vector2Int.Distance(firstEdge, secondEdge);
    }

    static List<Vector2Int> RelaxMidpointSpline(LineData v, List<Vector2Int> points, float relaxStrength)
    {
        Vector2Int vecAB = points[points.Count - 1] - points[0];
        float lenAB = math.sqrt(vecAB.x * vecAB.x + vecAB.y * vecAB.y);
        Vector2Int normAB = new Vector2Int(Mathf.RoundToInt(vecAB.x / lenAB), Mathf.RoundToInt(vecAB.y / lenAB));

        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector2Int vecAC = points[i] - points[0];

            Vector2Int vecCD = Rejection(vecAC, vecAB);

            points[i] = points[0] + vecAC - ScaleVector(vecCD, relaxStrength);
        }

        return points;
    }

    static Vector2Int ScaleVector(Vector2Int input, float factor)
    {
        return new Vector2Int(Mathf.RoundToInt(factor * input.x), Mathf.RoundToInt(factor * input.y));
    }

    static Vector2Int Projection(Vector2Int a, Vector2Int b)
    {
        return ScaleVector(b, Vector2.Dot(a, b) / Vector2.Dot(b, b));
    }

    static Vector2Int Subtract(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x - b.x, a.y - b.y);
    }

    static Vector2Int Rejection(Vector2Int a, Vector2Int b)
    {
        return Subtract(a, Projection(a, b));
    }

    struct LineData
    {
        public LineData(int resolution, int minX, int minY, int xRange, int yRange, float slope, int intercept, int offset)
        {
            this.resolution = resolution;
            this.minX = minX;
            this.minY = minY;
            this.xRange = xRange;
            this.yRange = yRange;
            this.slope = slope;
            this.intercept = intercept;
            this.offset = offset;
        }

        public int resolution;
        public int minX;
        public int minY;
        public int xRange;
        public int yRange;
        public float slope;
        public int intercept;
        public int offset;
    }

    public static List<Vector2Int> ComputeLSF(
        Color32 colId, 
        float[] pixelsXF, 
        float[] pixelsYF, 
        float2 minXY, 
        float2 maxXY, 
        Texture2D provinceTex,
        out float[] midPointDistances,
        out bool isIdeal,
        int sampleResolution = 20,
        float minimumDistanceAlwaysHorizontal = 450f)
    {
        isIdeal = true;

        double[] xDouble = new double[pixelsXF.Length];
        double[] yDouble = new double[pixelsYF.Length];
        for (int i = 0; i < pixelsXF.Length; i++)
        {
            xDouble[i] = pixelsXF[i];
            yDouble[i] = pixelsYF[i];
        }

        System.Tuple<double, double> resH = SimpleRegression.Fit(xDouble, yDouble);
        int interceptH = Mathf.RoundToInt((float)resH.Item1 * provinceTex.width);

        System.Tuple<double, double> resV = SimpleRegression.Fit(yDouble, xDouble);
        int interceptV = Mathf.RoundToInt((float)resV.Item1 * provinceTex.height);

        float xRange = maxXY.x - minXY.x;
        float yRange = maxXY.y - minXY.y;

        int xRangeInt = Mathf.RoundToInt(xRange * provinceTex.width);
        int yRangeInt = Mathf.RoundToInt(yRange * provinceTex.height);

        int minX = Mathf.RoundToInt(minXY.x * provinceTex.width);
        int minY = Mathf.RoundToInt(minXY.y * provinceTex.height);

        //Sample the two lines we found using linear regression.
        Vector2Int[] horizontalLineSamples = GetLineSamples(
            false,
            sampleResolution,
            minX,
            minY,
            xRangeInt,
            yRangeInt,
            (float)resH.Item2,
            interceptH,
            0);

        Vector2Int[] verticalLineSamples = GetLineSamples(
            true,
            sampleResolution,
            minX,
            minY,
            xRangeInt,
            yRangeInt,
            (float)resV.Item2,
            interceptV,
            0);

        //Get the distance for which the line is inside of the province.
        float distanceH = GetLineThroughProvinceColorDistance(
            sampleResolution,
            provinceTex,
            horizontalLineSamples,
            colId);

        
        float distanceV = GetLineThroughProvinceColorDistance(
            sampleResolution,
            provinceTex,
            verticalLineSamples,
            colId);

        //see which line is longer
        float slope;
        int intercept;
        float originalDistance;
        bool vertical;

        if (distanceH >= distanceV || distanceH >= minimumDistanceAlwaysHorizontal)
        {
            slope = (float)resH.Item2;
            intercept = interceptH;
            vertical = false;
            originalDistance = distanceH;
        } else
        {
            slope = (float)resV.Item2;
            intercept = interceptV;
            vertical = true;
            originalDistance = distanceV;
        }

        //Create some lines parallel to the original line and compare distances
        int numParallelLines = 2; //every couple of lines are equally offset but on opposite sides
        float[] lineDistances = new float[1 + numParallelLines];
        lineDistances[0] = originalDistance;
        LineData[] lines = new LineData[1 + numParallelLines];
        //add original line
        lines[0] = new LineData(
            sampleResolution,
            minX,
            minY,
            xRangeInt,
            yRangeInt,
            slope,
            intercept,
            0);


        float offsetAmount = 0f;
        for (int i = 1; i <= numParallelLines; i++)
        {
            //LINE
            int sign = i % 2 == 0 ? -1 : 1;
            offsetAmount = i % 2 == 0 ? offsetAmount : offsetAmount + 0.1f;
            int offset = Mathf.RoundToInt(i * offsetAmount * xRangeInt) * sign;
            verticalLineSamples = GetLineSamples(
                vertical,
                sampleResolution,
                minX,
                minY,
                xRangeInt,
                yRangeInt,
                slope,
                intercept,
                offset);
            lines[i] = new LineData(
                sampleResolution,
                minX,
                minY,
                xRangeInt,
                yRangeInt,
                slope,
                intercept,
                offset);

            //Put the distances of each line in an array.
            lineDistances[i] = GetLineThroughProvinceColorDistance(
                sampleResolution,
                provinceTex,
                verticalLineSamples,
                colId);
        }

        //find the max distance line
        float maxDistance = 0f;
        int longestLineIndex = 0;
        for (int i = 0; i < lineDistances.Length; i++)
        {
            if (lineDistances[i] > maxDistance)
            {
                maxDistance = lineDistances[i];
                longestLineIndex = i;
            }
        }
        
        //along the longest line, create some offsets and sample lines perpendicular to those offsets
        LineData[] offsetLines = new LineData[3];
        Vector2Int[][] offsetLineSamples = new Vector2Int[3][];

        Vector2Int[] offsetPointOnLineArray = new Vector2Int[3];

        for (int i = 1; i <= 3; i++)
        {
            offsetLineSamples[i - 1] = new Vector2Int[sampleResolution];

            Vector2Int a = GetPointOnLine(lines[longestLineIndex], ((float)i / ((float)3 + 1)), vertical);
            offsetPointOnLineArray[i - 1] = a;

            /*GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale /= 3f;
            go.transform.position = new Vector3(a.x * (100f / 8192f), 0f, a.y * (100f / 8192f));*/

            float perpSlope = -1 * lines[longestLineIndex].slope;
            int offsetIntercept = GetInterceptThroughPoint(perpSlope, a, !vertical);

            offsetLines[i - 1] = new LineData(
                sampleResolution,
                minX,
                minY,
                xRangeInt,
                yRangeInt,
                perpSlope,
                offsetIntercept,
                0);

            offsetLineSamples[i - 1] = GetLineSamples(
                !vertical,
                sampleResolution,
                minX,
                minY,
                xRangeInt,
                yRangeInt,
                perpSlope,
                offsetIntercept,
                0);
        }

        //get the midpoint of each perpendicular offset line
        List<Vector2Int> offsetMidpoints = new List<Vector2Int>();
        float[] offSetLineDistances = new float[3];

        for (int i = 0; i < 3; i++)
        {
            if (TryGetLineMidpointOverProvince(sampleResolution, offsetLineSamples[i], provinceTex, colId, out offSetLineDistances[i], out Vector2Int o))
            {
                offsetMidpoints.Add(o);
            }
        }

        //relax the midpoints a bit
        if (offsetMidpoints.Count == 3)
        {
            offsetMidpoints = RelaxMidpointSpline(lines[longestLineIndex], offsetMidpoints, 0.5f);
        }
        else if (offsetMidpoints.Count == 0 || offsetMidpoints.Count == 2)
        {
            isIdeal = false;

            int count = offsetMidpoints.Count;

            offsetMidpoints.Clear();

            float dist = Vector2.Distance(offsetPointOnLineArray[0], offsetPointOnLineArray[1]);

            for (int i = 0; i < offsetPointOnLineArray.Length; i++)
            {
                offsetMidpoints.Add(offsetPointOnLineArray[i]);
                offSetLineDistances[i] = dist;
            }

            ImproveCenteringOfTwoPointCase(offsetPointOnLineArray[0], offsetPointOnLineArray[2], provinceTex, colId, out offsetMidpoints, 40);
        }

        midPointDistances = offSetLineDistances;
        return offsetMidpoints;
    }
}
