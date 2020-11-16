﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using OpenSage.Data.Apt;
using OpenSage.Data.Apt.Characters;
using OpenSage.Data.Apt.FrameItems;
using OpenSage.Gui.Apt;
using OpenSage.Mathematics;

namespace OpenSage.Tools.AptEditor
{
    internal static class SampleApt
    {
        public static AptFile Create(string name, in ColorRgba color)
        {
            const int width = 100;
            const int height = 100;
            var aptFile = AptFile.CreateEmpty(name, width, height, 33);

            var backgroundShape = CreateShape(aptFile, new[]
            {
                "c",
                $"s s:{color.R}:{color.G}:{color.B}:{color.A}",
                $"t 0:0:{width}:{height}:0:{height}",
                $"t 0:0:{width}:{height}:{width}:0"
            });

            var backgroundShape2 = CreateShape(aptFile, new[]
            {
                "c",
                $"s s:{255 - color.R}:{255 - color.G}:{255 - color.B}:{color.A}",
                $"t 0:0:{width}:{height}:0:{height}",
                $"t 0:0:{width}:{height}:{width}:0"
            });

            var white = ColorRgba.White;
            var triSize = 10;
            var triangle = CreateShape(aptFile, new[]
            {
                "c",
                $"s s:{white.R}:{white.G}:{white.B}:{white.A}",
                $"t 0:{triSize / 2}:{triSize / 2}:0:{triSize}:{triSize / 2}",
            });
            var translatedPlace = PlaceObject.CreatePlace(1, triangle);
            translatedPlace.SetTransform(new ItemTransform
            {
                GeometryRotation = Matrix3x2.Identity,
                GeometryTranslation = new Vector2(20, 0),
                ColorTransform = ColorRgbaF.Red
            });
            var sprite = Sprite.Create(aptFile, new List<Frame>
            {
                Frame.Create(new List<FrameItem>
                {
                    PlaceObject.CreatePlace(0, triangle),
                    translatedPlace,
                })
            });

            var rotatedSprite = PlaceObject.CreatePlace(1, sprite);
            rotatedSprite.SetTransform(new ItemTransform
            {
                GeometryRotation = Matrix3x2.CreateRotation(MathF.PI / 2),
                GeometryTranslation = new Vector2(20, 20),
                ColorTransform = ColorRgbaF.White
            });

            var example1 = Sprite.Create(aptFile, new List<Frame>
            {
                Frame.Create(new List<FrameItem>
                {
                    PlaceObject.CreatePlace(0, backgroundShape),
                    rotatedSprite
                })
            });
            var example2 = Sprite.Create(aptFile, new List<Frame>
            {
                Frame.Create(new List<FrameItem>
                {
                    PlaceObject.CreatePlace(0, backgroundShape2),
                    rotatedSprite
                })
            });
            var frame = Frame.Create(new List<FrameItem>
            {
                PlaceObject.CreatePlace(0, example1),
                PlaceObject.CreatePlace(1, example2),
            });
            aptFile.Movie.Frames.Add(frame);

            return aptFile;
        }

        public static int CreateShape(AptFile apt, string[] geometryCommands)
        {
            using var reader = new StringReader(string.Join('\n', geometryCommands));
            var geometry = Geometry.Parse(apt, reader);
            var geometryId = (uint) apt.GeometryMap.Count;
            apt.GeometryMap.Add(geometryId, geometry);
            return Shape.Create(apt, geometryId);
        }
    }
}
