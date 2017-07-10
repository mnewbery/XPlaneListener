using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace TestConverter
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestInput()
        {

            Debug.WriteLine(BitConverter.ToSingle(new byte[] { 0, 0, 16, 64 }, 0));
            Debug.WriteLine(BitConverter.ToSingle(new byte[] { 0, 232, 16, 64 }, 0));
            Debug.WriteLine(BitConverter.ToSingle(new byte[] { 165, 232, 16, 64 }, 0));
            Debug.WriteLine(BitConverter.ToSingle(new byte[] { 25,	150,	118,	193}, 0));
            Debug.WriteLine(BitConverter.ToSingle(new byte[] { 18, 0, 0, 0 }, 0));
            Debug.WriteLine(BitConverter.ToSingle(new byte[] { 20, 0, 0, 0 }, 0));
            Debug.WriteLine(BitConverter.ToSingle(new byte[] { 21, 0, 0, 0 }, 0));

            Debug.WriteLine(BitConverter.ToSingle(new byte[] {68,	65,	84,	65},0));
           





        }
                                  

        [TestMethod]
        public void TestPitch()
        {
            float [] testValues = {
//2.85766f,
//2.66421f,
//2.55360f,
//2.48572f,
//2.43850f,
//2.40094f

    34280.1875f
    ,-295.46466f
    ,-51065.03125f


        };
            foreach (float d in testValues)
            {
                byte[] output = System.BitConverter.GetBytes(d);
                Debug.Write(string.Format("original: {0:0.00000} ", d));
                foreach (byte b in output)
                {
                    Debug.Write(string.Format("{0:000} ",b));
                }
                Debug.WriteLine("");
            }
        }
        //-0.4894
        [TestMethod]
        public void TestRoll()
        {

            byte[] output = System.BitConverter.GetBytes(-0.4894f);
            foreach (byte b in output)
            {
                Debug.WriteLine(b);
            }
        }

    }
}
