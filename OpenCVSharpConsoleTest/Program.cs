using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace OpenCVSharpConsoleTest
{
	class Program
	{
		static void Main(string[] args)
		{
			using (IplImage src = new IplImage("img/sample.jpg", LoadMode.AnyDepth | LoadMode.AnyColor))
			using (IplImage dst = new IplImage(src.Size, BitDepth.U8, src.NChannels))
			{
				float[] data = new float[] { 2,2,2,2,2,2,2,2,2,2,
																		 1,1,1,1,1,1,1,1,1,1,1
				};
				CvMat kernel = new CvMat(1, 21, MatrixType.F32C1, data);
				Cv.Normalize(kernel, kernel, 1.0, 0, NormType.L1);
				Cv.Filter2D(src, dst, kernel, new CvPoint(0, 0));
				
				CvWindow.ShowImages(src, dst);
			}
		}
	}
}
