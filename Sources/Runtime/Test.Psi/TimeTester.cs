// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TimeTester
    {
        [TestMethod]
        [Timeout(60000)]
        public void Time_GetTimeFromElapsedTicks()
        {
            // The clock sync precision in ticks
            long syncPrecision = 10;

            // Get QPC frequency and compute a sync precision in QPC cycles
            long qpcFrequency = Platform.Specific.TimeFrequency();
            double qpcToHns = 10000000.0 / qpcFrequency;
            long qpcSyncPrecision = (long)(syncPrecision / qpcToHns);

            // Max clock drift in ticks
            long maxDrift = 10000;

            // Force an initial tick calibration
            Time.GetTimeFromElapsedTicks(0);

            // Get QPC and system time, ensuring that both calls are within the specified tolerance
            long qpc, qpc0;
            DateTime now;
            do
            {
                qpc0 = Platform.Specific.TimeStamp();
                now = Time.GetCurrentTime();
                qpc = Platform.Specific.TimeStamp();
            }
            while ((qpc - qpc0) > qpcSyncPrecision);

            // Convert the raw QPC value into 100-nanosecond ticks, then use Time.GetTimeFromElapsedTicks
            // to convert this to a DateTime value.
            long nowInElapsedTicks = (long)(qpc * qpcToHns);
            DateTime nowFromElapsedTicks = Time.GetTimeFromElapsedTicks(nowInElapsedTicks);

            // Check that the difference between the two is no more than the max clock drift
            TimeSpan deviation = (nowFromElapsedTicks - now).Duration();
            Assert.IsTrue(deviation.Ticks <= maxDrift);

            // Sleep for a bit, then check times again
            Thread.Sleep(20);

            // Get current time from current elapsed ticks
            long nowInElapsedTicks2 = (long)(Platform.Specific.TimeStamp() * qpcToHns);
            DateTime nowFromElapsedTicks2 = Time.GetTimeFromElapsedTicks(nowInElapsedTicks2);

            // Check that both times advanced by approximately the same amount
            TimeSpan diffTicks = TimeSpan.FromTicks(nowInElapsedTicks2 - nowInElapsedTicks);
            TimeSpan diffTimes = nowFromElapsedTicks2 - nowFromElapsedTicks;

            // Allow for twice maxDrift since each of the converted values could be off by maxDrift
            Assert.IsTrue((diffTimes - diffTicks).Duration().Ticks <= (2 * maxDrift));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Time_TickCalibration()
        {
            TickCalibration cal = new TickCalibration(256, 10, 100000);
            double qpcToHns = 10000000.0 / Platform.Specific.TimeFrequency();
            long ticks0 = (long)(Platform.Specific.TimeStamp() * qpcToHns);
            long ft0 = Platform.Specific.SystemTime();

            // Calibrate and verify conversions in both directions in time
            cal.AddCalibrationData(ticks0, ft0);
            Assert.AreEqual(ft0, cal.ConvertToFileTime(ticks0, false));
            Assert.AreEqual(ft0 + 1, cal.ConvertToFileTime(ticks0 + 1, false));
            Assert.AreEqual(ft0 + 2, cal.ConvertToFileTime(ticks0 + 2, false));

            // Simulate a system clock advancement with the next calibration point
            long ticks1 = ticks0 + 100;
            long ft1 = ft0 + 200;

            // Calibrate using this new point
            cal.AddCalibrationData(ticks1, ft1);
            Assert.AreEqual(ft1, cal.ConvertToFileTime(ticks1, false));
            Assert.AreEqual(ft1 + 1, cal.ConvertToFileTime(ticks1 + 1, false));
            Assert.AreEqual(ft1 + 2, cal.ConvertToFileTime(ticks1 + 2, false));
            Assert.AreEqual(ft1 - 101, cal.ConvertToFileTime(ticks1 - 1, false));
            Assert.AreEqual(ft1 - 102, cal.ConvertToFileTime(ticks1 - 2, false));

            // Simulate a system clock regression with the next calibration point
            long ticks2 = ticks1 + 100;
            long ft2 = ft1 + 50;

            // Calibrate using this new point
            cal.AddCalibrationData(ticks2, ft2);

            // Ticks prior to ticks2 should use the previous calibration data
            Assert.AreEqual(ft1 + 49, cal.ConvertToFileTime(ticks2 - 51, false));

            // Because the system clock regressed, time conversions for the latter half will be clamped
            // to ft1 + 50 (i.e. ft2) until the tick counter catches up.
            Assert.AreEqual(ft1 + 50, cal.ConvertToFileTime(ticks2 - 50, false));
            Assert.AreEqual(ft1 + 50, cal.ConvertToFileTime(ticks2 - 49, false));
            Assert.AreEqual(ft1 + 50, cal.ConvertToFileTime(ticks2 - 1, false));

            // Ticks from ticks2 onwards should use the latest calibration data
            Assert.AreEqual(ft2, cal.ConvertToFileTime(ticks2, false));
            Assert.AreEqual(ft2 + 1, cal.ConvertToFileTime(ticks2 + 1, false));
            Assert.AreEqual(ft2 + 2, cal.ConvertToFileTime(ticks2 + 2, false));

            // Simulate a system clock jump with the next calibration point
            long ticks3 = ticks2 + 100;
            long ft3 = ft2 + 200;

            // Calibrate using this new point
            cal.AddCalibrationData(ticks3, ft3);

            // Ticks prior to ticks3 should use the previous calibration data
            Assert.AreEqual(ft2 + 99, cal.ConvertToFileTime(ticks3 - 1, false));

            // Ticks from ticks3 onwards use the latest calibration data
            Assert.AreEqual(ft3, cal.ConvertToFileTime(ticks3, false));
            Assert.AreEqual(ft3 + 1, cal.ConvertToFileTime(ticks3 + 1, false));
            Assert.AreEqual(ft3 + 2, cal.ConvertToFileTime(ticks3 + 2, false));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Time_TickCalibrationStability()
        {
            TickCalibration cal = new TickCalibration(256, 10, 100000);
            double qpcToHns = 10000000.0 / Platform.Specific.TimeFrequency();

            long ft0_0 = cal.ConvertToFileTime(0, false);
            long ft0_1 = cal.ConvertToFileTime(1, false);

            // Multiple consecutive calls should return the same result
            Assert.AreEqual(ft0_0, cal.ConvertToFileTime(0, false));
            Assert.AreEqual(ft0_0, cal.ConvertToFileTime(0, false));
            Assert.AreEqual(ft0_1, cal.ConvertToFileTime(1, false));
            Assert.AreEqual(ft0_1, cal.ConvertToFileTime(1, false));

            long ticks1 = (long)(Platform.Specific.TimeStamp() * qpcToHns);
            long ft1 = Platform.Specific.SystemTime();

            // Add new calibration data and verify stability of previous converted times
            cal.AddCalibrationData(ticks1, ft1);
            Assert.AreEqual(ft0_0, cal.ConvertToFileTime(0, false));
            Assert.AreEqual(ft0_1, cal.ConvertToFileTime(1, false));

            long ticks2 = (long)(Platform.Specific.TimeStamp() * qpcToHns);
            long ft2 = cal.ConvertToFileTime(ticks2, false);
            long ft2_1 = cal.ConvertToFileTime(ticks2 + 1, false);

            // Simulate system clock regressing, recalibrate, then verify stability of previous conversions
            cal.AddCalibrationData(ticks2 + 10, ft2 - 10 * TimeSpan.TicksPerSecond);
            Assert.AreEqual(ft2, cal.ConvertToFileTime(ticks2, false));
            Assert.AreEqual(ft2_1, cal.ConvertToFileTime(ticks2 + 1, false));

            long ticks3 = (long)(Platform.Specific.TimeStamp() * qpcToHns);
            long ft3 = cal.ConvertToFileTime(ticks3, false);
            long ft3_1 = cal.ConvertToFileTime(ticks3 + 1, false);

            // Simulate system clock jumping forward, recalibrate, then verify stability of previous conversions
            cal.AddCalibrationData(ticks3 + 10, ft3 + 10 * TimeSpan.TicksPerSecond);
            Assert.AreEqual(ft3, cal.ConvertToFileTime(ticks3, false));
            Assert.AreEqual(ft3_1, cal.ConvertToFileTime(ticks3 + 1, false));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Time_TickCalibrationMonotonicity()
        {
            TickCalibration cal = new TickCalibration(256, 10, 100000);
            double qpcToHns = 10000000.0 / Platform.Specific.TimeFrequency();

            // Converted system times for progressively increasing ticks should also progress
            Assert.IsTrue(cal.ConvertToFileTime(1, false) >= cal.ConvertToFileTime(0, false));
            Assert.IsTrue(cal.ConvertToFileTime(2, false) >= cal.ConvertToFileTime(1, false));
            Assert.IsTrue(cal.ConvertToFileTime(3, false) <= cal.ConvertToFileTime(4, false));
            Assert.IsTrue(cal.ConvertToFileTime(4, false) <= cal.ConvertToFileTime(5, false));

            long ticks = (long)(Platform.Specific.TimeStamp() * qpcToHns);
            long ft = Platform.Specific.SystemTime();

            // Add new calibration data and verify monotonicity both before and after calibration point
            cal.AddCalibrationData(ticks, ft);
            Assert.IsTrue(cal.ConvertToFileTime(ticks + 1, false) >= cal.ConvertToFileTime(ticks, false));
            Assert.IsTrue(cal.ConvertToFileTime(ticks + 2, false) >= cal.ConvertToFileTime(ticks + 1, false));
            Assert.IsTrue(cal.ConvertToFileTime(ticks - 1, false) <= cal.ConvertToFileTime(ticks, false));
            Assert.IsTrue(cal.ConvertToFileTime(ticks - 2, false) <= cal.ConvertToFileTime(ticks - 1, false));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Time_TickCalibrationCapacity()
        {
            // Initialize calibration with capacity of 4
            TickCalibration cal = new TickCalibration(4, 10, 100000);
            double qpcToHns = 10000000.0 / Platform.Specific.TimeFrequency();

            long ticks = (long)(Platform.Specific.TimeStamp() * qpcToHns);

            // Convert ticks to system file time and verify that it is stable
            long ft = cal.ConvertToFileTime(ticks);
            Assert.AreEqual(ft, cal.ConvertToFileTime(ticks));

            // Add more calibration data until the capacity is reached. After initialization,
            // converter will already have one calibration entry, so these add to it. Note
            // the different adjustment factor (2:1) that will allow us to distinguish this
            // from the previous calibration data.
            cal.AddCalibrationData(ticks + 10, ft + 20);
            cal.AddCalibrationData(ticks + 20, ft + 40);
            cal.AddCalibrationData(ticks + 30, ft + 60);
            Assert.AreEqual(ft + 60, cal.ConvertToFileTime(ticks + 30));
            Assert.AreEqual(ft + 40, cal.ConvertToFileTime(ticks + 20));
            Assert.AreEqual(ft + 20, cal.ConvertToFileTime(ticks + 10));
            Assert.AreEqual(ft, cal.ConvertToFileTime(ticks));

            // This will cause the first calibration point to be removed.
            cal.AddCalibrationData(ticks + 40, ft + 80);
            Assert.AreEqual(ft + 80, cal.ConvertToFileTime(ticks + 40));
            Assert.AreEqual(ft + 60, cal.ConvertToFileTime(ticks + 30));
            Assert.AreEqual(ft + 40, cal.ConvertToFileTime(ticks + 20));
            Assert.AreEqual(ft + 20, cal.ConvertToFileTime(ticks + 10));

            // The following conversion will now be based off the earliest remaining calibration
            // point that was added at (ticks+10, ft+20), which will produce a different result.
            Assert.AreEqual(ft + 10, cal.ConvertToFileTime(ticks));

            // This will cause one more calibration point to be removed.
            cal.AddCalibrationData(ticks + 50, ft + 100);
            Assert.AreEqual(ft + 100, cal.ConvertToFileTime(ticks + 50));
            Assert.AreEqual(ft + 80, cal.ConvertToFileTime(ticks + 40));
            Assert.AreEqual(ft + 60, cal.ConvertToFileTime(ticks + 30));
            Assert.AreEqual(ft + 40, cal.ConvertToFileTime(ticks + 20));

            // The following conversions will now be based off the earliest remaining calibration
            // point that was added at (ticks+20, ft+40), which will produce different results.
            Assert.AreEqual(ft + 30, cal.ConvertToFileTime(ticks + 10));
            Assert.AreEqual(ft + 20, cal.ConvertToFileTime(ticks));

            cal.AddCalibrationData(ticks + 60, ft + 120);
            cal.AddCalibrationData(ticks + 70, ft + 140);
            cal.AddCalibrationData(ticks + 80, ft + 160);
            cal.AddCalibrationData(ticks + 90, ft + 180);
            cal.AddCalibrationData(ticks + 100, ft + 200);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Time_GetCurrentTime()
        {
            long qpcFrequency = Platform.Specific.TimeFrequency();
            long oneTick = (long)Math.Ceiling(qpcFrequency / 10000000.0); // qpc ticks per hns tick

            var clock = new Clock();
            for (long i = 0; i < 10000000; i++) // 10000000 iterations ~ 1 second
            {
                var t0 = clock.GetCurrentTime();

                var qpc0 = Platform.Specific.TimeStamp();
                while ((Platform.Specific.TimeStamp() - qpc0) < oneTick)
                {
                    // wait for at least one tick to have elapsed
                }

                var t1 = clock.GetCurrentTime();
                if (t1 <= t0)
                {
                    Console.WriteLine($"LAST: {t0.TimeOfDay}, CURRENT: {t1.TimeOfDay}");
                }

                // verify that current time reported by the clock has advanced
                Assert.IsTrue(t1 > t0);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Time_TimerTest()
        {
            var elapsed = TimeSpan.Zero;

            using (var p = Pipeline.Create())
            {
                // create a timer with the smallest possible increment
                var timer = Timers.Timer(p, TimeSpan.FromMilliseconds(1));

                timer.Do(t =>
                {
                    // verify that the timer ticks forward
                    Assert.IsTrue(t > elapsed);
                    elapsed = t;
                });

                p.RunAsync();
                p.WaitAll(TimeSpan.FromSeconds(1));
            }
        }
    }
}
