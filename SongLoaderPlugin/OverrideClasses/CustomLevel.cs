﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace SongLoaderPlugin.OverrideClasses
{
    public class CustomLevel : LevelSO, IScriptableObjectResetable
    {
        public CustomSongInfo customSongInfo { get; private set; }
        public bool AudioClipLoading { get; set; }
        public bool BPMAndNoteSpeedFixed { get; private set; }
        public void Init(CustomSongInfo newCustomSongInfo)
        {
            customSongInfo = newCustomSongInfo;
            _levelID = customSongInfo.GetIdentifier();
            _songName = customSongInfo.songName;
            _songSubName = customSongInfo.songSubName;
            _songAuthorName = customSongInfo.GetSongAuthor();
            _beatsPerMinute = customSongInfo.beatsPerMinute;
            _songTimeOffset = customSongInfo.songTimeOffset;
            _shuffle = customSongInfo.shuffle;
            _shufflePeriod = customSongInfo.shufflePeriod;
            _previewStartTime = customSongInfo.previewStartTime;
            _previewDuration = customSongInfo.previewDuration;
            _environmentSceneInfo = EnvironmentsLoader.GetSceneInfo(customSongInfo.environmentName);
            string _customEnvironment = customSongInfo.customEnvironment;
            string _customEnvironmentHash = customSongInfo.customEnvironmentHash;

        }

        public void SetAudioClip(AudioClip newAudioClip)
        {
            _audioClip = newAudioClip;
        }

        public void SetCoverImage(Sprite newCoverImage)
        {
            _coverImage = newCoverImage;
        }

        public void SetDifficultyBeatmaps(DifficultyBeatmap[] newDifficultyBeatmaps)
        {
            _difficultyBeatmaps = newDifficultyBeatmaps;
        }

        public void SetBeatmapCharacteristics(BeatmapCharacteristicSO[] newBeatmapCharacteristics)
        {
            _beatmapCharacteristics = newBeatmapCharacteristics;
        }

        public void FixBPMAndGetNoteJumpMovementSpeed()
        {
            if (BPMAndNoteSpeedFixed) return;
            var bpms = new Dictionary<float, int> { { _beatsPerMinute, 0 } };
            foreach (var diffLevel in customSongInfo.difficultyLevels)
            {
                if (string.IsNullOrEmpty(diffLevel.json)) continue;

                float? bpm, noteSpeed;
                Color? colorLeft, colorRight;
                int? noteJumpStartBeatOffset;
              
                var diffBeatmap = _difficultyBeatmaps.FirstOrDefault(x =>
                    diffLevel.difficulty.ToEnum(BeatmapDifficulty.Normal) == x.difficulty);
                var customBeatmap = diffBeatmap as CustomDifficultyBeatmap;
                if (customBeatmap == null) continue;

                customBeatmap.ParseDiffJson(diffLevel.json, out bpm, out noteSpeed, out noteJumpStartBeatOffset, out colorLeft, out colorRight);
                if (bpm.HasValue)
                {
                    if (bpms.ContainsKey(bpm.Value))
                    {
                        bpms[bpm.Value]++;
                    }
                    else
                    {
                        bpms.Add(bpm.Value, 1);
                    }
                }


                if (!noteSpeed.HasValue) return;
                customBeatmap.SetNoteJumpMovementSpeed(noteSpeed.Value);
                if (noteJumpStartBeatOffset.HasValue)
                    customBeatmap.SetNoteJumpStartBeatOffset(noteJumpStartBeatOffset.Value);

                try
                {
                    if (colorLeft.HasValue)
                    {
                        customBeatmap.SetLeftColor(colorLeft.Value);
                        customBeatmap.hasCustomColors = true;
                    }
                    if (colorRight.HasValue)
                    {
                        customBeatmap.SetRightColor(colorRight.Value);
                        customBeatmap.hasCustomColors = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

            }

            _beatsPerMinute = bpms.OrderByDescending(x => x.Value).First().Key;
            try
            {
                foreach (var difficultyBeatmap in _difficultyBeatmaps)
                {
                    var customBeatmap = difficultyBeatmap as CustomDifficultyBeatmap;
                    if (customBeatmap == null) continue;
                    customBeatmap.BeatmapDataSO.SetRequiredDataForLoad(_beatsPerMinute, _shuffle, _shufflePeriod);
                    customBeatmap.BeatmapDataSO.Load();
                }

                BPMAndNoteSpeedFixed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        public class CustomDifficultyBeatmap : DifficultyBeatmap
        {
            public Color colorLeft { get; private set; }
            public Color colorRight { get; private set; }
            internal bool hasCustomColors { get;  set; } = false;

            private List<string> Requirements = new List<string>();
            public System.Collections.ObjectModel.ReadOnlyCollection<string> requirements
            {
                get { return Requirements.AsReadOnly(); }
            }
            private List<string> Suggestions = new List<string>();
            public System.Collections.ObjectModel.ReadOnlyCollection<string> suggestions
            {
                get { return Suggestions.AsReadOnly(); }
            }
            private List<string> Warnings = new List<string>();
            public System.Collections.ObjectModel.ReadOnlyCollection<string> warnings
            {
                get { return Warnings.AsReadOnly(); }
            }
            private List<string> Information = new List<string>();
            public System.Collections.ObjectModel.ReadOnlyCollection<string> information
            {
                get { return Information.AsReadOnly(); }
            }


            public CustomDifficultyBeatmap(IBeatmapLevel parentLevel, BeatmapDifficulty difficulty, int difficultyRank, float noteJumpMovementSpeed, int noteJumpStartBeatOffset, BeatmapDataSO beatmapData) : base(parentLevel, difficulty, difficultyRank, noteJumpMovementSpeed, noteJumpStartBeatOffset, beatmapData)
            {

            }

            public CustomLevel customLevel
            {
                get { return level as CustomLevel; }
            }

            public CustomBeatmapDataSO BeatmapDataSO
            {
                get { return _beatmapData as CustomBeatmapDataSO; }
            }

            public void SetNoteJumpMovementSpeed(float newNoteJumpMovementSpeed)
            {
                _noteJumpMovementSpeed = newNoteJumpMovementSpeed;
            }
            public void SetNoteJumpStartBeatOffset(int newNoteJumpStartBeatOffset)
            {
                _noteJumpStartBeatOffset = newNoteJumpStartBeatOffset;
            }
            public void SetLeftColor(Color colorLeft)
            {
                this.colorLeft = colorLeft;
            }
            public void SetRightColor(Color colorRight)
            {
                this.colorRight = colorRight;
            }


            //This is quicker than using a JSON parser
            internal void ParseDiffJson(string json, out float? bpm, out float? noteJumpSpeed, out int? noteJumpStartBeatOffset, out Color? colorLeft, out Color? colorRight)
            {
                int value;
                bpm = null;
                noteJumpSpeed = null;
                noteJumpStartBeatOffset = null;
                colorLeft = null;
                colorRight = null;
                var split = json.Split(':');
                for (var i = 0; i < split.Length; i++)
                {
                    try
                    {
                        //BPM and NoteJump
                        if (split[i].Contains("_beatsPerMinute"))
                        {
                            bpm = Convert.ToSingle(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                        }

                        if (split[i].Contains("_noteJumpSpeed"))
                        {
                            noteJumpSpeed = Convert.ToSingle(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                        }
                        if (split[i].Contains("_noteJumpStartBeatOffset"))
                        {
                            noteJumpStartBeatOffset = (int)Convert.ToDouble(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                        }

                        //Song Colors
                        if (split[i].Contains("_colorLeft"))
                        {
                            float? r = null;
                            float? g = null;
                            float? b = null;

                            if (split[i + 1].Contains("r"))
                                r = Convert.ToSingle(split[i + 2].Split(',')[0], CultureInfo.InvariantCulture);
                            if (split[i + 2].Contains("g"))
                                g = Convert.ToSingle(split[i + 3].Split(',')[0], CultureInfo.InvariantCulture);
                            if (split[i + 3].Contains("b"))
                                b = Convert.ToSingle(split[i + 4].Split('}')[0], CultureInfo.InvariantCulture);

                            if (!(r.Value < 0 || g.Value < 0 || b.Value < 0))
                                colorLeft = new Color(r.Value, g.Value, b.Value);
                        }
                        if (split[i].Contains("_colorRight"))
                        {
                            float? r = null;
                            float? g = null;
                            float? b = null;

                            if (split[i + 1].Contains("r"))
                                r = Convert.ToSingle(split[i + 2].Split(',')[0], CultureInfo.InvariantCulture);
                            if (split[i + 2].Contains("g"))
                                g = Convert.ToSingle(split[i + 3].Split(',')[0], CultureInfo.InvariantCulture);
                            if (split[i + 3].Contains("b"))
                                b = Convert.ToSingle(split[i + 4].Split('}')[0], CultureInfo.InvariantCulture);

                            if(!(r.Value < 0 || g.Value < 0 || b.Value < 0))
                            colorRight = new Color(r.Value, g.Value, b.Value);
                        }

                        //Requirements etc
                        if (split[i].Contains("_warnings"))
                        {
                            string[] reqs = split[i + 1].Split('[', ']')[1].Replace("\"", "").Split(',');
                            for (int j = 0; j < reqs.Length; j++)
                                AddWarning(reqs[j]);
                          

                        }

                        if (split[i].Contains("_requirements"))
                        {
                            string[] reqs = split[i + 1].Split('[', ']')[1].Replace("\"", "").Split(',');
                            for (int j = 0; j < reqs.Length; j++)
                                AddRequirement(reqs[j]);
                        }

                        if (split[i].Contains("_suggestions"))
                        {
                            string[] reqs = split[i + 1].Split('[', ']')[1].Replace("\"", "").Split(',');
                            for (int j = 0; j < reqs.Length; j++)
                                AddSuggestion(reqs[j]);
                        }
                        if (split[i].Contains("_information"))
                        {
                            string[] reqs = split[i + 1].Split('[', ']')[1].Replace("\"", "").Split(',');
                            for (int j = 0; j < reqs.Length; j++)
                                AddInformation(reqs[j]);
                        }
                     

                        //Check for Mapping Extensions Requirements
                        if (split[i].Contains("_lineIndex"))
                        {
                            value = Convert.ToInt32(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                            if ((value < 0 || value > 3) && !(value >= 1000 || value <= -1000)) AddRequirement("Mapping Extensions-More Lanes");
                            if (value >= 1000 || value <= -1000) AddRequirement("Mapping Extensions-Precision Placement");
                        }
                        if (split[i].Contains("_lineLayer"))
                        {
                            value = Convert.ToInt32(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                            if ((value < 0 || value > 2) && !(value >= 1000 || value <= -1000)) AddRequirement("Mapping Extensions-More Lanes");
                            if (value >= 1000 || value <= -1000) AddRequirement("Mapping Extensions-Precision Placement");
                        }
                        if (split[i].Contains("_cutDirection"))
                        {
                            value = Convert.ToInt32(split[i + 1].Split(',', '}')[0], CultureInfo.InvariantCulture);
                            if ((value >= 1000 && value <= 1360) || (value >= 2000 && value <= 2360))
                                AddRequirement("Mapping Extensions-Extra Note Angles");
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }





                }
            }

            public void AddRequirement(string requirement)
            {
                if (!Requirements.Contains(requirement))
                    Requirements.Add(requirement);
            }

            public void AddSuggestion(string suggestion)
            {
                if (!Suggestions.Contains(suggestion))
                    Suggestions.Add(suggestion);
            }

            public void AddWarning(string warning)
            {
                if (!Warnings.Contains(warning))
                    Warnings.Add(warning);
            }
            public void AddInformation(string info)
            {
                if (!Information.Contains(info))
                    Information.Add(info);
            }
        }

        public void Reset()
        {
            _audioClip = null;
            BPMAndNoteSpeedFixed = false;
        }

        public void SetSongColors(Color colorLeft, Color colorRight, bool hasColors)
        {
            if (!hasColors) return;
            EnvironmentColorsSetter colorSetter = new GameObject("SongLoader Color Setter").AddComponent<EnvironmentColorsSetter>();
            var scriptableColors = Resources.FindObjectsOfTypeAll<SimpleColorSO>();
            SimpleColorSO[] A = new SimpleColorSO[2];
            SimpleColorSO[] B = new SimpleColorSO[2];
            foreach (var color in scriptableColors)
            {
                int i = 0;
                if (color.name == "Color0")
                {
                    B[0] = color;
                    i++;
                }
                else if (color.name == "BaseColor0")
                {
                    A[0] = color;
                    i++;
                }
                else if (color.name == "Color1")
                {
                    A[1] = color;
                    i++;
                }
                else if (color.name == "BaseColor1")
                {
                    B[1] = color;
                    i++;
                }
            }
            colorSetter.SetPrivateField("_colorsA", A);
            colorSetter.SetPrivateField("_colorsB", B);
            colorSetter.SetPrivateField("_colorManager", Resources.FindObjectsOfTypeAll<ColorManager>().First());
            colorSetter.SetPrivateField("_overrideColorA", colorRight);
            colorSetter.SetPrivateField("_overrideColorB", colorLeft);
            colorSetter.Awake();
        }
    }
}
