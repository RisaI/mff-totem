using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.Common;
using FarseerPhysics.Common.Decomposition;

using ClipperLib;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;

namespace Mff.Totem.Core
{
	public class Terrain
	{
		const int CHUNK_CACHE = 64;

		public const int MAX_DEPTH = 10000 * 6, SPACING = 32;
		public const int BASE_HEIGHT = 0, BASE_STEP = 2048;

		public OpenSimplexNoise NoiseMap
		{
			get;
			private set;
		}

		long _seed;
		public long Seed
		{
			get { return _seed; }
			set { _seed = value; NoiseMap = new OpenSimplexNoise(value); }
		}

		public GameWorld World
		{
			get;
			private set;
		}

		public Dictionary<ulong, Chunk> SavedChunks = new Dictionary<ulong, Chunk>();
		public List<Chunk> ActiveChunks = new List<Chunk>();
		private List<Vertices> TriangulatedArea = new List<Vertices>();

		public Terrain(GameWorld world)
		{
			World = world;
		}

		public void Generate(long seed = 0)
		{
			if (seed == 0)
				seed = TotemGame.Random.Next();
			Seed = seed;

			SavedChunks.Clear();
			ActiveChunks.Clear();
			//SetActiveArea(Vector2.Zero, 1500);
		}

		public void Update()
		{

		}

		public void CreateDamage(Vector2 point)
		{
			int x = Helper.NegDivision((int)point.X, Chunk.CHUNK_SIZE_PX),
				y = Helper.NegDivision((int)point.Y, Chunk.CHUNK_SIZE_PX),
				tX = Helper.NegDivision(Helper.NegModulo((int)point.X, Chunk.CHUNK_SIZE_PX), Chunk.TILE_SIZE_PX),
				tY = Helper.NegDivision(Helper.NegModulo((int)point.Y, Chunk.CHUNK_SIZE_PX), Chunk.TILE_SIZE_PX);
			var chunk = GetChunk(x, y);
			chunk.SetDamage(tX, tY, true);
			var update = chunk.NeedsUpdate;
			Task.Run(() => PlaceChunk(chunk, x, y));
			if (update)
			{
				if (tX == 0)
					UpdateIfActive(x-1,y);
				if (tY == 0)
					UpdateIfActive(x, y - 1);
			}
		}

		private void UpdateIfActive(int x, int y)
		{
			var ch = GetChunk(x, y);
			if (ActiveChunks.Contains(ch))
			{
				ch.NeedsUpdate = true;
				Task.Run(() => { PlaceChunk(ch, x, y); });
			}
		}

		private int activeAreaX, activeAreaY, activeAreaRange;
		public void SetActiveArea(Vector2 position, float range)
		{
			bool changed = false;
			int half = (int)(range / Chunk.CHUNK_SIZE_PX) + 1;
			if (activeAreaRange != half * 2 + 1)
			{
				activeAreaRange = half * 2 + 1;
				changed = true;
			}
			if (activeAreaX != Helper.NegDivision((int)position.X, Chunk.CHUNK_SIZE_PX) - half)
			{
				activeAreaX = Helper.NegDivision((int)position.X, Chunk.CHUNK_SIZE_PX) - half;
				changed = true;
			}
			if (activeAreaY != Helper.NegDivision((int)position.Y, Chunk.CHUNK_SIZE_PX) - half)
			{
				activeAreaY = Helper.NegDivision((int)position.Y, Chunk.CHUNK_SIZE_PX) - half;
				changed = true;
			}

			if (!changed)
				return;

			TriangulatedArea.Clear();
			ActiveChunks.ForEach(ac => // Unload unchanged unactive chunks
			{
				var unpacked = TerrainHelper.UnpackCoordinates(ac.ID);
				if (unpacked.Item1 < activeAreaX || unpacked.Item1 >= activeAreaX + activeAreaRange ||
									  unpacked.Item2 < activeAreaY || unpacked.Item2 >= activeAreaY + activeAreaRange)
				{
					if (ac.Body != null)
					{
						lock (ac.Body)
						{
							World.Physics.RemoveBody(ac.Body);
						}
					}
					if (!ac.ShouldSave)
						SavedChunks.Remove(ac.ID);
				}
			});
			ActiveChunks.Clear();

			for (int x = 0; x < activeAreaRange; ++x)
			{
				for (int y = 0; y < activeAreaRange; ++y)
				{
					Chunk c = GetChunk(activeAreaX + x, activeAreaY + y);
					int aX = activeAreaX + x, aY = activeAreaY + y;
					ActiveChunks.Add(c);
					Task.Run(() =>
					{
						PlaceChunk(c, aX, aY);
					});
				}
			}
		}

		private void PlaceChunk(Chunk c, int x, int y)
		{
			if (c.NeedsUpdate)
			{
				Clipper clip = new Clipper();
				if (!c.Complete())
				{
					for (int tX = 0; tX < Chunk.CHUNK_SIZE - 1; ++tX)
					{
						for (int tY = 0; tY < Chunk.CHUNK_SIZE - 1; ++tY)
						{
							byte marchValue = 0;
							if (c.GetTile(tX, tY))
								marchValue += 1;
							if (c.GetTile(tX + 1, tY))
								marchValue += 2;
							if (c.GetTile(tX, tY + 1))
								marchValue += 4;
							if (c.GetTile(tX + 1, tY + 1))
								marchValue += 8;
							TerrainHelper.MarchingSquare(marchValue, new IntPoint(x * Chunk.CHUNK_SIZE_PX + tX * Chunk.TILE_SIZE_PX + Chunk.TILE_SIZE_PX / 2,
																				 y * Chunk.CHUNK_SIZE_PX + tY * Chunk.TILE_SIZE_PX + Chunk.TILE_SIZE_PX / 2), clip);
						}
					}
				}
				else
				{
					clip.AddPolygon(new List<IntPoint>() { 
						new IntPoint(x * Chunk.CHUNK_SIZE_PX + Chunk.TILE_SIZE_PX / 2,y * Chunk.CHUNK_SIZE_PX + Chunk.TILE_SIZE_PX / 2),
						new IntPoint((x + 1) * Chunk.CHUNK_SIZE_PX - Chunk.TILE_SIZE_PX / 2,y * Chunk.CHUNK_SIZE_PX + Chunk.TILE_SIZE_PX / 2),
						new IntPoint((x + 1) * Chunk.CHUNK_SIZE_PX - Chunk.TILE_SIZE_PX / 2, (y + 1) * Chunk.CHUNK_SIZE_PX - Chunk.TILE_SIZE_PX / 2),
						new IntPoint(x * Chunk.CHUNK_SIZE_PX + Chunk.TILE_SIZE_PX / 2, (y + 1) * Chunk.CHUNK_SIZE_PX - Chunk.TILE_SIZE_PX / 2) }, PolyType.ptSubject);
				}
				Chunk xNeighbour = null, yNeighbour = null;
				{
					xNeighbour = GetChunk(x + 1, y);
					for (int tY = 0; tY < Chunk.CHUNK_SIZE - 1; ++tY)
					{
						byte marchValue = 0;
						if (c.GetTile(Chunk.CHUNK_SIZE - 1, tY))
							marchValue += 1;
						if (xNeighbour.GetTile(0, tY))
							marchValue += 2;
						if (c.GetTile(Chunk.CHUNK_SIZE - 1, tY + 1))
							marchValue += 4;
						if (xNeighbour.GetTile(0, tY + 1))
							marchValue += 8;
						TerrainHelper.MarchingSquare(marchValue, new IntPoint((x + 1) * Chunk.CHUNK_SIZE_PX - Chunk.TILE_SIZE_PX / 2,
																			  y * Chunk.CHUNK_SIZE_PX + tY * Chunk.TILE_SIZE_PX + Chunk.TILE_SIZE_PX / 2), clip);
					}
				}
				{
					yNeighbour = GetChunk(x, y + 1);
					for (int tX = 0; tX < Chunk.CHUNK_SIZE - 1; ++tX)
					{
						byte marchValue = 0;
						if (c.GetTile(tX, Chunk.CHUNK_SIZE - 1))
							marchValue += 1;
						if (c.GetTile(tX + 1, Chunk.CHUNK_SIZE - 1))
							marchValue += 2;
						if (yNeighbour.GetTile(tX, 0))
							marchValue += 4;
						if (yNeighbour.GetTile(tX + 1, 0))
							marchValue += 8;
						TerrainHelper.MarchingSquare(marchValue, new IntPoint(x * Chunk.CHUNK_SIZE_PX + tX * Chunk.TILE_SIZE_PX + Chunk.TILE_SIZE_PX / 2,
						                                                     (y + 1) * Chunk.CHUNK_SIZE_PX - Chunk.TILE_SIZE_PX / 2), clip);
					}
				}
				{
					Chunk xyNeighbour = GetChunk(x + 1, y + 1);

					byte marchValue = 0;
					if (c.GetTile(Chunk.CHUNK_SIZE - 1, Chunk.CHUNK_SIZE - 1))
						marchValue += 1;
					if (xNeighbour.GetTile(0, Chunk.CHUNK_SIZE - 1))
						marchValue += 2;
					if (yNeighbour.GetTile(Chunk.CHUNK_SIZE - 1, 0))
						marchValue += 4;
					if (xyNeighbour.GetTile(0, 0))
						marchValue += 8;
					TerrainHelper.MarchingSquare(marchValue, new IntPoint((x + 1) * Chunk.CHUNK_SIZE_PX - Chunk.TILE_SIZE_PX / 2,
																		 (y + 1) * Chunk.CHUNK_SIZE_PX - Chunk.TILE_SIZE_PX / 2), clip);
				}
				List<Vertices> triangulation = new List<Vertices>();
				List<List<IntPoint>> result = new List<List<IntPoint>>();
				clip.Execute(ClipType.ctUnion, result, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
				result.ForEach(res => triangulation.AddRange(Helper.Triangulate(res)));

				c.RenderData = new VertexPositionColor[triangulation.Count * 3];
				int index = 0;
				triangulation.ForEach(v =>
				{
					c.RenderData[index++] = new VertexPositionColor(new Vector3(v[0].X, v[0].Y, 0), Color.White);
					c.RenderData[index++] = new VertexPositionColor(new Vector3(v[1].X, v[1].Y, 0), Color.White);
					c.RenderData[index++] = new VertexPositionColor(new Vector3(v[2].X, v[2].Y, 0), Color.White);
				});

				c.Polygons = new List<Vertices>();
				result.ForEach(res => c.Polygons.Add(Helper.PolygonToVertices(res)));
			}

			if (c.Body == null)
			{
				lock (World.Physics)
				{
					c.Body = BodyFactory.CreateBody(World.Physics, Vector2.Zero, 0, BodyType.Static, this);
				}
			}

			if (c.NeedsUpdate)
			{
				lock (World.Physics)
				{
					c.Body.FixtureList.ForEach(f => c.Body.DestroyFixture(f));
					c.Polygons.ForEach(p => FixtureFactory.AttachLoopShape(p, c.Body, this));
				}
			}
			c.NeedsUpdate = false;
		}

		public Chunk GetChunk(int Cx, int Cy)
		{
			ulong id = TerrainHelper.PackCoordinates(Cx, Cy);
			Chunk c = null;
			lock (SavedChunks)
			{
				if (SavedChunks.ContainsKey(id))
				{
					c = SavedChunks[id];
				}
				else
				{
					c = new Chunk(id);
					SavedChunks.Add(id, c);

					for (int tX = 0; tX < Chunk.CHUNK_SIZE; ++tX)
					{
						for (int tY = 0; tY < Chunk.CHUNK_SIZE; ++tY)
						{
							//c.TileMap.Set(tX + tY * Chunk.CHUNK_SIZE, TileMap(Cx * Chunk.CHUNK_SIZE + tX, Cy * Chunk.CHUNK_SIZE + tY));
							c.TileMap[tX + tY * Chunk.CHUNK_SIZE] = TileMap(Cx * Chunk.CHUNK_SIZE + tX, Cy * Chunk.CHUNK_SIZE + tY);
						}
					}
				}
			}
			return c;
		}


		public float HeightMap(float x)
		{
			return BASE_HEIGHT + (float)((NoiseMap.Evaluate(x / (Chunk.TILE_SIZE_PX * 32), 0)- 0.5) * BASE_STEP + 8 * NoiseMap.Evaluate(x / 128, Chunk.CHUNK_SIZE_PX));
		}

		public bool TileMap(int xT, int yT)
		{
			//return NoiseMap.Evaluate(xT, yT) < .1;
			return yT * Chunk.TILE_SIZE_PX > HeightMap(xT);
		}

		public ulong ChunkID(float x, float y)
		{
			return TerrainHelper.PackCoordinates(Helper.NegDivision((int)x, Chunk.CHUNK_SIZE_PX),
			                                             Helper.NegDivision((int)y, Chunk.CHUNK_SIZE_PX));
		}

		public static class TerrainHelper
		{
			public static ulong PackCoordinates(int x, int y)
			{
				return ((ulong)((uint)x) << 32) | (uint)y;
			}

			public static Tuple<int, int> UnpackCoordinates(ulong packed)
			{
				return Tuple.Create((int)(packed >> 32), (int)packed);
			}

			public static void MarchingSquare(byte v, IntPoint topleft, Clipper output)
			{
				switch (v)
				{
					case 1:
						Triangle(topleft, 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, 0)), 
						         topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX / 2)), output);
						break;
					case 2:
						Triangle(topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, 0)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, 0)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX / 2)), output);
						break;
					case 4:
						Triangle(topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX / 2)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, Chunk.TILE_SIZE_PX)), output);
						break;
					case 8:
						Triangle(topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX / 2)), output);
						break;

					case 3:
						Quad(topleft, topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, 0)),
						     topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX / 2)),
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX / 2)), output);
						break;
					case 12:
						Quad(topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX / 2)), 
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX / 2)),
						     topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX)), 
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX)), output);
						break;
					case 5:
						Quad(topleft, 
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, 0)),
						     topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX)), 
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, Chunk.TILE_SIZE_PX)), output);
						break;
					case 10:
						Quad(topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, 0)), 
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, 0)),
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, Chunk.TILE_SIZE_PX)), 
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX)), output);
						break;

					case 7:
						Pentagon(topleft, 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, 0)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX / 2)),
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX)), output);
						break;
					case 11:
						Pentagon(topleft, 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, 0)),
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX)),
								 topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX / 2)), output);
						break;
					case 13:
						Pentagon(topleft, 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, 0)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX / 2)),
								 topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX)), output);
						break;
					case 14:
						Pentagon(topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, 0)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, 0)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX)),
								 topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX / 2)), output);
						break;

					case 9:
						Triangle(topleft,
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, 0)), 
						         topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX / 2)), output);
						Triangle(topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX / 2)), output);
						break;
					case 6:
						Triangle(topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX)), 
						         topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX / 2)),
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, Chunk.TILE_SIZE_PX)), output);
						Triangle(topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, 0)), 
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX / 2)),
						         topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX / 2, 0)), output);
						break;

					case 15:
						Quad(topleft, 
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, 0)),
							 topleft.Add(new IntPoint(0, Chunk.TILE_SIZE_PX)), 
						     topleft.Add(new IntPoint(Chunk.TILE_SIZE_PX, Chunk.TILE_SIZE_PX)), output);
						break;
				}
			}

			public static void Triangle(IntPoint a, IntPoint b, IntPoint c, Clipper output)
			{
				output.AddPolygon(new List<IntPoint>() { a, b, c }, PolyType.ptSubject);
			}

			public static void Quad(IntPoint a, IntPoint b, IntPoint c, IntPoint d, Clipper output)
			{
				Triangle(a, b, c, output);
				Triangle(b, d, c, output);
			}

			public static void Pentagon(IntPoint a, IntPoint b, IntPoint c, IntPoint d, IntPoint e, Clipper output)
			{
				Triangle(a, b, c, output);
				Triangle(a, c, d, output);
				Triangle(a, d, e, output);
			}
		}
	}

	public class Chunk
	{
		public const int TILE_SIZE_PX = 16;
		public const int CHUNK_SIZE = 32,
			CHUNK_SIZE_PX = CHUNK_SIZE * TILE_SIZE_PX;

		public bool[] TileMap, DamageMap;
		public ulong ID;
		public Task GenerationTask;
		public Body Body;
		public VertexPositionColor[] RenderData;
		public List<Vertices> Polygons;

		public bool ShouldSave
		{
			get;
			private set;
		}

		public bool NeedsUpdate;

		public Chunk(int x, int y) :
			this(Terrain.TerrainHelper.PackCoordinates(x, y))
		{

		}

		public Chunk(ulong id)
		{
			ID = id;
			NeedsUpdate = true;
			TileMap = new bool[CHUNK_SIZE * CHUNK_SIZE];
			DamageMap = new bool[CHUNK_SIZE * CHUNK_SIZE];
		}

		public bool GetDamage(int tX, int tY)
		{
			return DamageMap[tX + tY * CHUNK_SIZE];
		}

		public bool GetTile(int tX, int tY)
		{
			return GetTile(tX + tY * CHUNK_SIZE);
		}

		public bool GetTile(int i)
		{
			return TileMap[i] && !DamageMap[i];
		}

		public bool Complete()
		{
			for (int i = 0; i < CHUNK_SIZE * CHUNK_SIZE; ++i)
				if (!GetTile(i))
					return false;
			return true;
		}

		public void SetDamage(int tX, int tY, bool val)
		{
			if (tX >= CHUNK_SIZE || tY >= CHUNK_SIZE)
				return;
			
			if (DamageMap[tX + tY * CHUNK_SIZE] != val)
				NeedsUpdate = true;
			DamageMap[tX + tY * CHUNK_SIZE] = val;
			if (val)
				ShouldSave = true;
		}

		public Rectangle BoundingBox
		{
			get {
				var unpacked = Terrain.TerrainHelper.UnpackCoordinates(ID);
				return new Rectangle(unpacked.Item1 * CHUNK_SIZE_PX, 
				                     unpacked.Item2 * CHUNK_SIZE_PX, 
				                     CHUNK_SIZE_PX, CHUNK_SIZE_PX); }
		}
	}
}