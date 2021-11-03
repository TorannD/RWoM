using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public static class SkullDatabase
    {
		private class HeadGraphicRecord
		{
			public Gender gender;

			public CrownType crownType;

			public string graphicPath;

			private List<KeyValuePair<Color, Graphic_Multi>> graphics = new List<KeyValuePair<Color, Graphic_Multi>>();

			public HeadGraphicRecord(string graphicPath)
			{
				this.graphicPath = graphicPath;
				string[] array = Path.GetFileNameWithoutExtension(graphicPath).Split('_');
				try
				{
					crownType = ParseHelper.FromString<CrownType>(array[array.Length - 2]);
					gender = ParseHelper.FromString<Gender>(array[array.Length - 3]);
				}
				catch (Exception ex)
				{
					Log.Error("Parse error with head graphic at " + graphicPath + ": " + ex.Message);
					crownType = CrownType.Undefined;
					gender = Gender.None;
				}
			}

			public Graphic_Multi GetGraphic(Color color, bool dessicated = false, bool skinColorOverriden = false)
			{
				Shader shader = ((!dessicated) ? ShaderUtility.GetSkinShader(skinColorOverriden) : ShaderDatabase.Cutout);
				for (int i = 0; i < graphics.Count; i++)
				{
					if (color.IndistinguishableFrom(graphics[i].Key) && graphics[i].Value.Shader == shader)
					{
						return graphics[i].Value;
					}
				}
				Graphic_Multi graphic_Multi = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(graphicPath, shader, Vector2.one, color);
				graphics.Add(new KeyValuePair<Color, Graphic_Multi>(color, graphic_Multi));
				return graphic_Multi;
			}
		}

		public static string CustomSkullsPath => "Skeletons/Pawn/Humanlike/Heads/Dessicated";

		private static List<HeadGraphicRecord> customSkulls;

		static SkullDatabase()
		{
			RebuildAllGraphic();
		}

		private static void RebuildAllGraphic()
        {
			customSkulls = new List<HeadGraphicRecord>();

			ModContentPack modContentPack = LoadedModManager.GetMod<TorannMagicMod>().Content;
			foreach (string folder in modContentPack.foldersToLoadDescendingOrder)
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(folder, Path.Combine(GenFilePaths.ContentPath<Texture2D>(), CustomSkullsPath)));
				if (!directoryInfo.Exists)
				{
					continue;
				}

				foreach (FileInfo item in directoryInfo.GetFiles())
				{
					string[] array2 = Path.GetFileNameWithoutExtension(item.FullName).Split('_');
					string text = "";
					if (array2.Length <= 2)
					{
						text = array2[0];
					}
					else if (array2.Length == 3)
					{
						text = array2[0] + "_" + array2[1];
					}
					else if (array2.Length == 4)
					{
						text = array2[0] + "_" + array2[1] + "_" + array2[2];
					}

					customSkulls.Add(new HeadGraphicRecord(CustomSkullsPath + "/" + text));
				}
			}
		}

		public static Graphic_Multi GetSkullFor(Gender gender, CrownType crownType)
        {
			foreach (HeadGraphicRecord graphic in customSkulls)
            {
				if(graphic.gender == gender && graphic.crownType == crownType)
                {
					return graphic.GetGraphic(Color.white, dessicated: true);
				}
            }
			foreach (HeadGraphicRecord graphic in customSkulls)
			{
				if (graphic.gender == Gender.None && graphic.crownType == crownType)
				{
					return graphic.GetGraphic(Color.white, dessicated: true);
				}
			}

			return customSkulls.First().GetGraphic(Color.white, dessicated: true);
		}
    }
}
