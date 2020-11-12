﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenSage.Data.Apt;
using OpenSage.Data.Apt.Characters;
using OpenSage.Data.Apt.FrameItems;
using OpenSage.Gui.Apt.ActionScript.Opcodes;
using OpenSage.Mathematics;
using OpenSage.Tools.AptEditor.UI;
using Action = OpenSage.Data.Apt.FrameItems.Action;

namespace OpenSage.Tools.AptEditor.Apt.Editor
{
    internal class LogicalPlaceObject
    {
        public bool IsRemoveObject { get; set; }
        public bool ModifyingExisting;
        public int? Character { get; private set; }
        public Matrix3x2? Transform { get; private set; }
        public ColorRgba? ColorTransform { get; private set; }
        public float? Ratio { get; private set; }
        public string Name;
        public List<ClipEvent> ClipEvents;

        public LogicalPlaceObject(FrameItem frameItem)
        {
            if (frameItem is RemoveObject)
            {
                IsRemoveObject = true;
                return;
            }

            var placeObject = (PlaceObject) frameItem;
            ModifyingExisting = placeObject.Flags.HasFlag(PlaceObjectFlags.Move);

            if (placeObject.Flags.HasFlag(PlaceObjectFlags.HasCharacter))
            {
                Character = placeObject.Character;
            }

            if (placeObject.Flags.HasFlag(PlaceObjectFlags.HasMatrix))
            {
                var mx22 = placeObject.RotScale;
                var v2 = placeObject.Translation;
                Transform = new Matrix3x2(mx22.M11, mx22.M12, mx22.M21, mx22.M22, v2.X, v2.Y);
            }

            if (placeObject.Flags.HasFlag(PlaceObjectFlags.HasColorTransform))
            {
                ColorTransform = placeObject.Color;
            }

            if (placeObject.Flags.HasFlag(PlaceObjectFlags.HasRatio))
            {
                Ratio = placeObject.Ratio;
            }

            if (placeObject.Flags.HasFlag(PlaceObjectFlags.HasName))
            {
                Name = placeObject.Name;
            }

            if (placeObject.Flags.HasFlag(PlaceObjectFlags.HasClipAction))
            {
                ClipEvents = placeObject.ClipEvents;
            }
        }
    }

    internal class LogicalAction
    {
        public LogicalInstructions Instructions { get; set; }
        public LogicalAction(Action action)
        {
            Instructions = new LogicalInstructions(action.Instructions);
        }
    }

    internal class LogicalInitAction
    {
        public uint Sprite { get; private set; }
        public LogicalInstructions Instructions { get; private set; }
        public LogicalInitAction(InitAction initAction)
        {
            Sprite = initAction.Sprite;
            Instructions = new LogicalInstructions(initAction.Instructions);
        }
    }

    internal class FrameItemUtilities
    {
        public bool Active => _manager.CurrentCharacter is Playable p && p.Frames.Count > 0;
        public List<FrameItem> CurrentItems => _storedFrame.FrameItems;
        public IReadOnlyList<(int, LogicalPlaceObject)> PlaceObjects => _placeObjects;
        public IReadOnlyList<FrameLabel> FrameLabels => _frameLabels;
        public IReadOnlyList<LogicalAction> FrameActions => _frameActions;
        public IReadOnlyList<LogicalInitAction> InitActions => _initActions;
        public IReadOnlyList<BackgroundColor> BackgroundColors => _backgroundColors;

        private List<(int, LogicalPlaceObject)> _placeObjects;
        private List<FrameLabel> _frameLabels;
        private List<LogicalAction> _frameActions;
        private List<LogicalInitAction> _initActions;
        private List<BackgroundColor> _backgroundColors;
        private AptSceneManager _manager;
        private Frame _storedFrame;

        // Return true if it's a new frame
        public bool Reset(AptSceneManager manager, bool force = false)
        {
            _manager = manager;
            if (!Active)
            {
                return false;
            }

            if (!(_manager.CurrentCharacter is Playable p && _manager.CurrentFrameWrapped.HasValue))
            {
                return false;
            }

            var currentFrame = p.Frames[_manager.CurrentFrameWrapped.Value];
            if (!force && ReferenceEquals(currentFrame, _storedFrame))
            {
                return false;
            }

            _storedFrame = currentFrame;
            _placeObjects = new List<(int, LogicalPlaceObject)>();
            _frameLabels = new List<FrameLabel>();
            _frameActions = new List<LogicalAction>();
            _initActions = new List<LogicalInitAction>();
            _backgroundColors = new List<BackgroundColor>();
            foreach (var frameItem in CurrentItems)
            {
                switch (frameItem)
                {
                    case PlaceObject placeObject:
                        _placeObjects.Add((placeObject.Depth, new LogicalPlaceObject(placeObject)));
                        break;
                    case RemoveObject removeObject:
                        _placeObjects.Add((removeObject.Depth, new LogicalPlaceObject(removeObject)));
                        break;
                    case FrameLabel frameLabel:
                        _frameLabels.Add(frameLabel);
                        break;
                    case Action action:
                        _frameActions.Add(new LogicalAction(action));
                        break;
                    case InitAction initAction:
                        _initActions.Add(new LogicalInitAction(initAction));
                        break;
                    case BackgroundColor background:
                        _backgroundColors.Add(background);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return true;
        }

        private static Vector2 Projection(Vector2 vector, Vector2 projectOn) => Vector2.Dot(projectOn, vector) * projectOn / projectOn.LengthSquared();

        private static float MatrixMaxDifference(in Matrix3x2 m1, in Matrix3x2 m2, bool checkTranslation)
        {
            var diff = m1 - m2;
            if (checkTranslation == false)
            {
                diff.Translation = Vector2.Zero;
            }
            var diffs = new[] { diff.M11, diff.M12, diff.M21, diff.M22, diff.M31, diff.M32 };
            return diffs.Select(value => MathF.Abs(value)).Max();
        }

        public static Matrix3x2 CreateTransformation(float rotation, float skew, Vector2 scale, Vector2 translation)
        {
            return Matrix3x2.CreateRotation(rotation) * Matrix3x2.CreateSkew(0, skew) * Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(translation);
        }

        public static (float, float, Vector2) GetRotationSkewAndScale(in Matrix3x2 matrix, float errorTolerance = float.Epsilon)
        {
            // QR Decomposition

            var c1 = new Vector2(matrix.M11, matrix.M21);
            var c2 = new Vector2(matrix.M12, matrix.M22);

            // Columns of Q Matrix
            var e1 = Vector2.Normalize(c1);
            var e2 = Vector2.Normalize(c2 - Projection(c2, c1));

            var qMatrix = new Matrix3x2(e1.X, e2.X, e1.Y, e2.Y, 0, 0);
            var rMatrix = new Matrix3x2(Vector2.Dot(c1, e1), Vector2.Dot(c2, e1), 0, Vector2.Dot(c2, e2), 0, 0);

            var largestDifference = MatrixMaxDifference(qMatrix * rMatrix, matrix, false);
            if (largestDifference > errorTolerance)
            {
                throw new ArithmeticException();
            }

            var rotation = MathF.Atan2(qMatrix.M12, qMatrix.M11);
            var skew = rMatrix.M12;
            var scale = new Vector2(rMatrix.M11, rMatrix.M22);

            var check = CreateTransformation(rotation, skew, scale, matrix.Translation);
            var checkDifference = MatrixMaxDifference(check, matrix, false);
            if (largestDifference > errorTolerance)
            {
                throw new ArithmeticException();
            }

            return (rotation, skew, scale);
        }
    }
}