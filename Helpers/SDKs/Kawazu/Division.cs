﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using NMeCab.Specialized;

namespace Kawazu
{
    /// <summary>
    /// The division from the word separator.
    /// </summary>
    public class Division : List<JapaneseElement>
    {
        public string Surface
        {
            get
            {
                var builder = new StringBuilder();
                foreach (var element in this)
                {
                    builder.Append(element.Element);
                }
                
                return builder.ToString();
            }
        }
        
        public string HiraReading
        {
            get
            {
                var builder = new StringBuilder();
                foreach (var element in this)
                {
                    builder.Append(element.HiraNotation);
                }

                return builder.ToString();
            }
        }
        public string HiraPronunciation
        {
            get
            {
                var builder = new StringBuilder();
                foreach (var element in this)
                {
                    builder.Append(element.HiraPronunciation);
                }

                return builder.ToString();
            }
        }

        public string KataReading => Utilities.ToRawKatakana(HiraReading);
        public string KataPronunciation => Utilities.ToRawKatakana(HiraPronunciation);
        public string RomaReading => Utilities.ToRawRomaji(HiraReading);
        public string RomaPronunciation => Utilities.ToRawRomaji(HiraPronunciation);

        public readonly string PartsOfSpeech;
        public readonly string PartsOfSpeechSection1;
        public readonly string PartsOfSpeechSection2;
        public readonly string PartsOfSpeechSection3;

        public readonly bool IsEndsInTsu;

        public Division(MeCabIpaDicNode node, TextType type, RomajiSystem system = RomajiSystem.Hepburn)
        {
            PartsOfSpeech = node.PartsOfSpeech;
            PartsOfSpeechSection1 = node.PartsOfSpeechSection1;
            PartsOfSpeechSection2 = node.PartsOfSpeechSection2;
            PartsOfSpeechSection3 = node.PartsOfSpeechSection3;
            IsEndsInTsu = node.Surface.Last() == 'っ' || node.Surface.Last() == 'ッ';

            switch (type)
            {
                case TextType.PureKana:
                    if (node.Surface.Length == node.Pronounciation.Length)
                        for (var i = 0; i < node.Surface.Length; i++)
                            Add(new JapaneseElement(node.Surface[i].ToString(), Utilities.ToRawKatakana(node.Surface[i].ToString()), node.Pronounciation[i].ToString(), TextType.PureKana, system));
                    else
                        for (var i = 0; i < node.Surface.Length; i++)
                        {
                            var surface = Utilities.ToRawKatakana(node.Surface[i].ToString());
                            Add(new JapaneseElement(node.Surface[i].ToString(), surface, surface, TextType.PureKana, system));
                        }
                    break;
                
                case TextType.PureKanji:
                    Add(new JapaneseElement(node.Surface, node.Reading, node.Pronounciation, TextType.PureKanji, system));
                    break;
                
                case TextType.KanjiKanaMixed:
                    var surfaceBuilder = new StringBuilder(node.Surface);
                    var readingBuilder = new StringBuilder(node.Reading);
                    var pronunciationBuilder = new StringBuilder(node.Pronounciation);
                    var kanasInTheEnd = new StringBuilder();
                    while (Utilities.IsKana(surfaceBuilder[0])) // Pop the kanas in the front.
                    {
                        Add(new JapaneseElement(surfaceBuilder[0].ToString(), Utilities.ToRawKatakana(surfaceBuilder[0].ToString()), pronunciationBuilder[0].ToString(), TextType.PureKana, system));
                        surfaceBuilder.Remove(0, 1);
                        readingBuilder.Remove(0, 1);
                        pronunciationBuilder.Remove(0, 1);
                    }
                    
                    while (Utilities.IsKana(surfaceBuilder[surfaceBuilder.Length - 1])) // Pop the kanas in the end.
                    {
                        kanasInTheEnd.Append(surfaceBuilder[surfaceBuilder.Length - 1].ToString());
                        surfaceBuilder.Remove(surfaceBuilder.Length - 1, 1);
                        readingBuilder.Remove(readingBuilder.Length - 1, 1);
                        pronunciationBuilder.Remove(pronunciationBuilder.Length - 1, 1);
                    }

                    if (Utilities.HasKana(surfaceBuilder.ToString())) // For the middle part:
                    {
                        var previousIndex = -1;
                        var kanaIndex = 0;
                        
                        var kanas = from ele in surfaceBuilder.ToString()
                            where Utilities.IsKana(ele) && ele != 'ヶ' && ele != 'ケ'
                            select ele;
                        
                        var kanaList = kanas.ToList();

                        if (kanaList.Count == 0)
                        {
                            Add(new JapaneseElement(surfaceBuilder.ToString(), readingBuilder.ToString(), pronunciationBuilder.ToString(), TextType.PureKanji, system));
                            break;
                        }

                        var globalIndex = 0;
                        var surf = surfaceBuilder.ToString();
                        for (int i = 0; i < surf.Length; ++i)
                        {
                            var ch = surf[i];
                            if (Utilities.IsKanji(ch))
                            {
                                if (kanaIndex >= kanaList.Count)
                                {
                                    var start = previousIndex + 1;
                                    var end = readingBuilder.Length - previousIndex - 1;
                                    var add = ch.ToString();

                                    //double kanji
                                    while (i + 1 < surf.Length && Utilities.IsKanji(surf[i + 1]))
                                    {
                                        i++;
                                        add += surf[i].ToString();
                                    }

                                    Add(new JapaneseElement(add, readingBuilder.ToString(start, end), pronunciationBuilder.ToString(start, end), TextType.PureKanji, system));
                                    continue;
                                }

                                if (i != surf.Length - 1)
                                {
                                    var index = readingBuilder.ToString()
                                        .IndexOf(Utilities.ToRawKatakana(kanaList[kanaIndex].ToString()), StringComparison.Ordinal);

                                    if (index == 0)
                                        index++;

                                    var start = previousIndex + 1;
                                    var length = index - previousIndex - 1;
                                    var add = ch.ToString();

                                    //double kanji
                                    while (i + 1 < surf.Length && Utilities.IsKanji(surf[i + 1]))
                                    {
                                        i++;
                                        add += surf[i].ToString();
                                    }

                                    Add(new JapaneseElement(add, readingBuilder.ToString(start, length), pronunciationBuilder.ToString(start, length), TextType.PureKanji, system));
                                    previousIndex = index;
                                    kanaIndex++;
                                    globalIndex += length;
                                }
                                else // end one
                                {
                                    var start = globalIndex;
                                    var length = readingBuilder.ToString().Length - start;
                                    Add(new JapaneseElement(ch.ToString(), readingBuilder.ToString(start, length), pronunciationBuilder.ToString(start, length), TextType.PureKanji, system));
                                }
                            }

                            if (Utilities.IsKana(ch))
                            {
                                var kana = Utilities.ToRawKatakana(ch.ToString());
                                Add(new JapaneseElement(ch.ToString(), kana, kana, TextType.PureKana, system));
                                globalIndex++;
                            }
                        }
                    }

                    else
                    {
                        Add(new JapaneseElement(surfaceBuilder.ToString(), readingBuilder.ToString(), pronunciationBuilder.ToString(), TextType.PureKanji, system));
                    }

                    if (kanasInTheEnd.Length != 0)
                    {
                        for (var i = kanasInTheEnd.Length - 1; i >= 0; i--)
                        {
                            var kana = Utilities.ToRawKatakana(kanasInTheEnd.ToString()[i].ToString());
                            Add(new JapaneseElement(kanasInTheEnd.ToString()[i].ToString(), kana, kana, TextType.PureKana, system));
                        }
                    }
                    break;
                
                case TextType.Others:
                    Add(new JapaneseElement(node.Surface, node.Surface, node.Pronounciation, TextType.Others, system));
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}