﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;

namespace PiCamCV.ConsoleApp.Runners.PanTilt.MoveStrategies
{
    public class CameraModifierStrategy : IPanTiltModifierStrategy
    {
        private Point _target;
        private Point _objective;
        private readonly IScreen _screen;
        private readonly decimal _xDiffScale, _yDiffScale;

        private readonly static ILog Log = LogManager.GetCurrentClassLogger();

        public CameraModifierStrategy(CaptureConfig captureConfig, IScreen screen, Point objective, Point target)
        {
            _objective = objective;
            _target = target;
            _screen = screen;

            // calibration was done in 320x240. If capture settings different need to scale the calibration
            _xDiffScale = captureConfig.Width /320m;
            _yDiffScale = captureConfig.Height /240m;
        }
        public PanTiltSetting CalculateNewSetting( PanTiltSetting currentSetting)
        {
            var newSetting = currentSetting.Clone();

            var xDiff = (_target.X - _objective.X) * _xDiffScale;
            var yDiff = (_target.Y - _objective.Y) * _yDiffScale;

            const int deadZone = 10;
            /*
             * Linear regression calculated from observation
             * https://github.com/neutmute/PiCamCV/blob/master/docs/panTiltCalibration.xlsx
             */
            var xDeflection = Math.Abs(xDiff) > deadZone ? 0.0933m * xDiff + 0.0864m : 0;
            var yDeflection = Math.Abs(yDiff) > deadZone ? 0.0812m * yDiff + 0.2091m : 0;

            newSetting.PanPercent += xDeflection;
            newSetting.TiltPercent += yDeflection;

            var message1 = string.Format(
                "Target={0}, Objective={1}"
                , _target
                , _objective
                );

            var message2 = string.Format(
                "Moving {0} -> {1}"
                , currentSetting
                , newSetting
                );

            _screen.WriteLine(message1);
            _screen.WriteLine(message2);
            Log.Info(message1);
            Log.Info(message2);

            return newSetting;
        }
    }
}
