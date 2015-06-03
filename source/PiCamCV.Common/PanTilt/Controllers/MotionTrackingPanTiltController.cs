﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Kraken.Core;
using PiCamCV.Common.ExtensionMethods;
using PiCamCV.Common.Interfaces;
using PiCamCV.ConsoleApp.Runners.PanTilt;
using PiCamCV.ExtensionMethods;

namespace PiCamCV.Common.PanTilt.Controllers
{
    public class MotionTrackingPanTiltOutput : CameraPanTiltProcessOutput
    {
        public List<MotionSection> MotionSections { get; private set; }

        public MotionSection TargetedMotion { get;set;}

        public bool IsDetected
        {
            get { return MotionSections.Count > 0; }
        }

        public MotionTrackingPanTiltOutput()
        {
            MotionSections = new List<MotionSection>();
        }
    }

    public class MotionTrackingPanTiltController : CameraBasedPanTiltController<MotionTrackingPanTiltOutput>
    {
        private readonly IScreen _screen;

        private readonly Timer _timerUntilMotionSettled;
        public MotionDetectSettings Settings { get; set; }

        private readonly MotionDetector _motionDetector;

        public MotionTrackingPanTiltController(IPanTiltMechanism panTiltMech, CaptureConfig captureConfig, IScreen screen)
            : base(panTiltMech, captureConfig)
        {
            _screen = screen;
            _motionDetector = new MotionDetector();

            ServoSettleTime = TimeSpan.FromMilliseconds(200);

            _timerUntilMotionSettled = new Timer(1000);
            _timerUntilMotionSettled.AutoReset = false;
            _timerUntilMotionSettled.Elapsed += (o, a) =>
            {
                _screen.WriteLine("Motion settled");
                IsServoInMotion = false;
            };

            ServoSettleTimeChanged += (o, a) =>
            {
                _screen.WriteLine("Servo settle time changed to {0}", ServoSettleTime.ToHumanReadable());
                _timerUntilMotionSettled.Interval = ServoSettleTime.TotalMilliseconds;
            };
        }

        protected override MotionTrackingPanTiltOutput DoProcess(CameraProcessInput input)
        {
            var detectorInput = new MotionDetectorInput();
            detectorInput.SetCapturedImage = false;
            detectorInput.Settings = Settings;
            detectorInput.Captured = input.Captured;

            var motionOutput = _motionDetector.Process(detectorInput);

            var targetPoint = CentrePoint;
            MotionSection biggestMotion = null;
            
            if (motionOutput.IsDetected)
            {
                _screen.BeginRepaint();
                biggestMotion = motionOutput.BiggestMotion;
                targetPoint = biggestMotion.Region.Center();
            }

            var output = ReactToTarget(targetPoint);
            if (IsServoInMotion)
            {
                _screen.WriteLine("Reacting to target {0}, size {1}", targetPoint, biggestMotion.Region.Area());
            }
            output.MotionSections.AddRange(motionOutput.MotionSections);

            if (biggestMotion != null)
            {
                output.TargetedMotion = motionOutput.BiggestMotion;
            }

            return output;
        }

        protected override void PostServoSettle()
        {
            IsServoInMotion = true;
            _screen.WriteLine("Servo moved, awaiting motion settle");
            _motionDetector.Reset();
            _timerUntilMotionSettled.Start();
        }
    }
}
