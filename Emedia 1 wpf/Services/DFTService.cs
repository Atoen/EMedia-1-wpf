using OpenCvSharp;

namespace Emedia_1_wpf.Services;

public class DFTService
{
    public void DisplayDFT(string filePath)
    {
        using var image = Cv2.ImRead(filePath, ImreadModes.Grayscale);
        using var padded = new Mat();

        var rows = Cv2.GetOptimalDFTSize(image.Rows);
        var columns = Cv2.GetOptimalDFTSize(image.Cols);
        
        Cv2.CopyMakeBorder(image, padded, 0, rows - image.Rows, 0, columns - image.Cols, BorderTypes.Constant, Scalar.All(0));

        var paddedF32 = new Mat();
        padded.ConvertTo(paddedF32, MatType.CV_32F);
        
        using var zeros = Mat.Zeros(padded.Size(), MatType.CV_32F);
        var planes = new Mat[] { paddedF32, zeros };

        using var complex = new Mat();
        Cv2.Merge(planes, complex);

        using var dft = new Mat();
        Cv2.Dft(complex, dft);

        Cv2.Split(dft, out var dftPlanes);

        using var magnitude = new Mat();
        Cv2.Magnitude(dftPlanes[0], dftPlanes[1], magnitude);
        Cv2.Log(magnitude + Scalar.All(1), magnitude);

        using var spectrum = magnitude[new Rect(0, 0, magnitude.Cols & -2, magnitude.Rows & -2)];
        
        var cx = spectrum.Cols / 2;
        var cy = spectrum.Rows / 2;
        
        using var q0 = new Mat(spectrum, new Rect(0, 0, cx, cy));   // Top-Left
        using var q1 = new Mat(spectrum, new Rect(cx, 0, cx, cy));  // Top-Right
        using var q2 = new Mat(spectrum, new Rect(0, cy, cx, cy));  // Bottom-Left
        using var q3 = new Mat(spectrum, new Rect(cx, cy, cx, cy)); // Bottom-Right
        
        using var tmp = new Mat();                           
        q0.CopyTo(tmp);
        q3.CopyTo(q0);
        tmp.CopyTo(q3);
        
        q1.CopyTo(tmp);                    
        q2.CopyTo(q1);
        tmp.CopyTo(q2);
        
        Cv2.Normalize(spectrum, spectrum, 0, 1, NormTypes.MinMax); 
        
        Cv2.ImShow("Input Image", image);
        Cv2.ImShow("Spectrum Magnitude", spectrum);
        
        using var inverseTransform = new Mat();
        Cv2.Dft(dft, inverseTransform, DftFlags.Inverse | DftFlags.RealOutput);
        Cv2.Normalize(inverseTransform, inverseTransform, 0, 1, NormTypes.MinMax);
        Cv2.ImShow("Reconstructed by Inverse DFT", inverseTransform);
    }
}