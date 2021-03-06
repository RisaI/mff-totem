﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClipperLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Physics2D.Common;
using Physics2D.Common.Decomposition;
using Physics2D.Dynamics;

namespace Mff.Totem.Core
{
	[Serializable("terrain_wcomponent")]
	public class TerrainComponent : WorldComponent, IUpdatable, IDrawable
	{
		const int CHUNK_CACHE = 15;

		public const int SPACING = 32;
		public const int BASE_HEIGHT = 0, BASE_STEP = 2048;

		public OpenSimplexNoise NoiseMap
		{
			get;
			private set;
		}

		public Dictionary<ulong, Chunk> ChunkCache = new Dictionary<ulong, Chunk>();
		public Chunk[] ActiveChunks = new Chunk[CHUNK_CACHE * CHUNK_CACHE];

		Clipper c;

		public long Seed
		{
			get;
			set;
		}

		public TerrainComponent()
		{
			c = new Clipper();
		}

		public override void Initialize()
		{
			NoiseMap = new OpenSimplexNoise(Seed);
			c.Clear();

			World.Game.OnResolutionChange -= PrepareEffect;
			World.Game.OnResolutionChange += PrepareEffect;

			PrepareEffect(
				(int)World.Game.Resolution.X,
				(int)World.Game.Resolution.Y
			);
		}

		/// <summary>
		/// Damage the specified points.
		/// </summary>
		/// <param name="points">Points.</param>
		public void Damage(params IntPoint[] points)
		{
			Damage(points.ToList());
		}

		/// <summary>
		/// Damage the specified points.
		/// </summary>
		/// <param name="points">Points.</param>
		public void Damage(List<IntPoint> points)
		{
			damageQueue.Add(points);
		}

		/// <summary>
		/// Applies the damage to affected chunks.
		/// </summary>
		/// <param name="damage">Damage.</param>
		private void ApplyDamage(List<IntPoint> damage)
		{
			long minX = long.MaxValue, minY = long.MaxValue,
				maxX = long.MinValue, maxY = long.MinValue;

			for (int i = 0; i < damage.Count; ++i)
			{
				if (damage[i].X < minX)
					minX = damage[i].X;
				if (damage[i].X > maxX)
					maxX = damage[i].X;

				if (damage[i].Y < minY)
					minY = damage[i].Y;
				if (damage[i].Y > maxY)
					maxY = damage[i].Y;
			}

			minX = Helper.NegDivision(minX, Chunk.SIZE);
			maxX = Helper.NegDivision(maxX, Chunk.SIZE);
			minY = Helper.NegDivision(minY, Chunk.SIZE);
			maxY = Helper.NegDivision(maxY, Chunk.SIZE);

			var clipper = new Clipper();
			for (int y = (int)minY; y <= maxY; ++y)
			{
				for (int x = (int)minX; x <= maxX; ++x)
				{
					var chunk = GetChunk(TerrainHelper.PackCoordinates(x, y));

					clipper.Clear();
					clipper.AddPolygon(new List<IntPoint>() {
						new IntPoint(chunk.Left, chunk.Top),
						new IntPoint(chunk.Left + Chunk.SIZE, chunk.Top),
						new IntPoint(chunk.Left + Chunk.SIZE, chunk.Top + Chunk.SIZE),
						new IntPoint(chunk.Left, chunk.Top + Chunk.SIZE)
					}, PolyType.ptSubject);
					clipper.AddPolygon(damage, PolyType.ptClip);
					clipper.AddPolygons(chunk.Damage, PolyType.ptClip);
					clipper.Execute(ClipType.ctIntersection, chunk.Damage,
									PolyFillType.pftNonZero, PolyFillType.pftNonZero);

					chunk.Recalculate = true;
					if (ActiveChunks.Contains(chunk))
						Task.Run(() => { PlaceChunk(chunk); });
				}
			}
		}

		List<List<IntPoint>> damageQueue = new List<List<IntPoint>>();

		/// <summary>
		/// Updates the terrain.
		/// </summary>
		public void Update(GameTime gameTime)
		{
			// Apply damage from the damage queue
			damageQueue.ForEach(d => ApplyDamage(d));
			damageQueue.Clear();
		}

		Effect GroundEffect;
		void PrepareEffect(int width, int height)
		{
			if (GroundEffect == null)
				GroundEffect = ContentLoader.Shaders["ground"];
			GroundEffect.Parameters["Projection"].SetValue(Matrix.CreateOrthographic(width, -height, 0, 1));
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			switch (World.DrawLayer)
			{
				case 0:
					DrawBackground(spriteBatch);
					break;
				case 1:
					DrawForeground(spriteBatch);
					break;
			}
		}

		public void DrawBackground(SpriteBatch spriteBatch)
		{
			GroundEffect.Parameters["View"].SetValue(World.Camera.ViewMatrix *
				Matrix.CreateTranslation(-World.Game.Resolution.X / 2, -World.Game.Resolution.Y / 2, 0));
			GroundEffect.Parameters["Texture"].SetValue(ContentLoader.Textures["dirt"]);

			for (int i = 0; i < ActiveChunks.Length; ++i)
			{
				var chunk = ActiveChunks[i];
				if (chunk == null || chunk.State != ChunkStateEnum.Placed)
					continue;

				foreach (EffectPass pass in GroundEffect.Techniques[0].Passes)
				{
					pass.Apply();
					World.Game.GraphicsDevice.DrawUserPrimitives(
						PrimitiveType.TriangleList,
						chunk.TriangulatedBackgroundVertices,
						0, chunk.TriangulatedBackgroundVertices.Length / 3);
				}
			}
		}

		public void DrawForeground(SpriteBatch spriteBatch)
		{
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend,
							  SamplerState.PointClamp, null, null, null,
							  World.Camera != null ? World.Camera.ViewMatrix : Matrix.Identity);
			for (int i = 0; i < ActiveChunks.Length; ++i)
			{
				var chunk = ActiveChunks[i];
				if (chunk == null || chunk.State != ChunkStateEnum.Placed)
					continue;

				foreach (EffectPass pass in GroundEffect.Techniques[0].Passes)
				{
					pass.Apply();
					World.Game.GraphicsDevice.DrawUserPrimitives(
						PrimitiveType.TriangleList,
						chunk.TriangulatedForegroundVertices,
						0, chunk.TriangulatedForegroundVertices.Length / 3);
				}
				foreach (Chunk.GrassPoint g in chunk.GrassPoints)
				{
					var texture = ContentLoader.Textures["grass"];
					spriteBatch.Draw(texture, g.Position, null, Color.Green, g.Rotation,
									 new Vector2(texture.Width / 2, texture.Height),
									 1.2f, SpriteEffects.None, 1f);
				}
			}
			spriteBatch.End();
		}

		/// <summary>
		/// Get surface height on x.
		/// </summary>
		/// <returns>The height.</returns>
		/// <param name="x">The x coordinate.</param>
		public float HeightMap(float x)
		{
			return BASE_HEIGHT + (float)((NoiseMap.Evaluate(x / (Chunk.SIZE * 32), 0) - 0.5) *
										 BASE_STEP + 8 * NoiseMap.Evaluate(x / 128, Chunk.SIZE));
		}

		/// <summary>
		/// Get surface normal on x.
		/// </summary>
		/// <returns>The normal.</returns>
		/// <param name="x">The x coordinate.</param>
		public Vector2 Normal(float x)
		{
			var v = new Vector2(HeightMap(x + 1f) - HeightMap(x - 1f), -2f);
			v.Normalize();
			return v;
		}

		public float[] TreesInChunkX(int chunkX)
		{
			Random rand = new Random((int)(chunkX * Seed));
			float[] result = new float[rand.Next(0, 8)];
			for (int i = 0; i < result.Length; ++i)
			{
				float x = Chunk.SIZE * (chunkX + (float)rand.NextDouble());
				while (true)
				{
					for (int a = 0; a < i; ++a)
					{
						if (Math.Abs(result[a] - x) < 32)
						{
							x = Chunk.SIZE * (chunkX + (float)rand.NextDouble());
							continue;
						}
					}
					break;
				}
				result[i] = x;
			}
			return result;
		}

		/// <summary>
		/// Get the ID of a chunk at a given position.
		/// </summary>
		/// <returns>ID.</returns>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		public ulong ChunkID(float x, float y)
		{
			return TerrainHelper.PackCoordinates(Helper.NegDivision((int)x, Chunk.SIZE),
												 Helper.NegDivision((int)y, Chunk.SIZE));
		}

		public bool Multithreaded = true;
		int _activeX, _activeY;
		/// <summary>
		/// Sets a region around a point as active.
		/// </summary>
		/// <param name="center">Center.</param>
		public override void SetActiveArea(Vector2 center)
		{
			_activeX = Helper.NegDivision((int)center.X, Chunk.SIZE);
			_activeY = Helper.NegDivision((int)center.Y, Chunk.SIZE);

			Chunk[] newActive = new Chunk[CHUNK_CACHE * CHUNK_CACHE];
			for (int y = 0; y < CHUNK_CACHE; ++y)
			{
				for (int x = 0; x < CHUNK_CACHE; ++x)
				{
					var cX = x + _activeX - CHUNK_CACHE / 2;
					var cY = y + _activeY - CHUNK_CACHE / 2;
					var id = TerrainHelper.PackCoordinates(cX, cY);
					Chunk chunk = null;
					if (ActiveChunks.Any(c => c != null && c.ID == id))
					{
						chunk = ActiveChunks.First(c => c != null && c.ID == id);
					}
					else
					{
						chunk = GetChunk(id);
						if (Multithreaded)
							Task.Run(() => { PlaceChunk(chunk); });
						else
							PlaceChunk(chunk);
					}
					newActive[x + y * CHUNK_CACHE] = chunk;
				}
			}

			for (int i = 0; i < ActiveChunks.Length; ++i)
			{
				if (ActiveChunks[i] != null && !newActive.Contains(ActiveChunks[i]))
				{
					UnplaceChunk(ActiveChunks[i]);
					if (!ActiveChunks[i].ShouldSave)
						ChunkCache.Remove(ActiveChunks[i].ID);
				}
			}
			ActiveChunks = newActive;
		}

		/// <summary>
		/// Get chunk with id from registry or create new if not in registry yet.
		/// </summary>
		/// <returns>The chunk.</returns>
		/// <param name="id">Identifier.</param>
		/// <param name="onlyread">If set to <c>true</c>, chunk will not be added to chunk cache if created.</param>
		public Chunk GetChunk(ulong id, bool onlyread = false)
		{
			if (ChunkCache.ContainsKey(id))
				return ChunkCache[id];
			else
			{
				Chunk chunk = new Chunk(id);
				if (!onlyread)
					ChunkCache.Add(id, chunk);
				return chunk;
			}
		}

		/// <summary>
		/// Generate chunk data for a given chunk.
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		public void GenerateChunk(Chunk chunk)
		{
			if (chunk.State != ChunkStateEnum.Emtpy)
				return;

			long top = chunk.Top, left = chunk.Left;
			var clipper = new Clipper();

			chunk.Polygons = new List<List<IntPoint>>();

			// Add chunk bounding rectangle
			clipper.AddPolygon(new List<IntPoint>() {
				new IntPoint(left, top),
				new IntPoint(left + Chunk.SIZE, top),
				new IntPoint(left + Chunk.SIZE, top + Chunk.SIZE),
				new IntPoint(left, top + Chunk.SIZE)
			}, PolyType.ptSubject);

			List<IntPoint> surfacePolygon = new List<IntPoint>(Chunk.SIZE / SPACING + 3);
			for (int i = 0; i <= Chunk.SIZE / SPACING; ++i)
			{
				surfacePolygon.Add(new IntPoint(left + i * SPACING, (int)HeightMap(left + i * SPACING)));
			}
			surfacePolygon.Add(new IntPoint(left + Chunk.SIZE, top + Chunk.SIZE));
			surfacePolygon.Add(new IntPoint(left, top + Chunk.SIZE));
			clipper.AddPolygon(surfacePolygon, PolyType.ptClip);

			// Check midpoint for surface
			{
				var midpoint = HeightMap(left + Chunk.SIZE / 2);
				chunk.IsSurface = midpoint >= top && midpoint <= top + Chunk.SIZE;
			}

			// Add solution to chunk data
			clipper.Execute(ClipType.ctIntersection,
							chunk.Polygons,
							PolyFillType.pftNonZero,
							PolyFillType.pftNonZero);

			// Cave generation
			chunk.Cavities = new List<List<IntPoint>>();

			for (int i = 0; i < Chunk.SIZE / SPACING; ++i)
			{
				var x = left + SPACING / 2 + i * SPACING;
				var y = HeightMap(x) + 4;
				if (chunk.ContainsHeight(y))
				{
					chunk.GrassPoints.Add(new Chunk.GrassPoint()
					{
						Position = new Vector2(x, y),
						Rotation = MathHelper.PiOver2 - Helper.DirectionToAngle(Normal(x))
					});
				}
			}

			chunk.Recalculate = true;
			chunk.State = ChunkStateEnum.Generated;
		}

		/// <summary>
		/// Places the chunk and its contents in the game world.
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		public void PlaceChunk(Chunk chunk)
		{
			if (chunk.State == ChunkStateEnum.Emtpy) // Skip if empty
				GenerateChunk(chunk);

			chunk.Hulls.ForEach(h =>
								World.Lighting.Hulls.Remove(h));

			if (chunk.Recalculate) // Recalculate if needed
				chunk.Calculate(World);

			lock (World.Physics) // Lock the physics engine while manipulating chunk bodies.
			{
				lock (chunk)
				{
					if (chunk.Body != null)
						World.Physics.RemoveAsync(chunk.Body);

					chunk.Body = World.Physics.CreateBody(Vector2.Zero, 0, BodyType.Static);
					chunk.Body.Tag = this;

					chunk.PhysicsOutput.ForEach(o =>
					{
						var fixture = chunk.Body.CreateLoopShape(o);
						fixture.Tag = this;
					});
					chunk.Hulls.ForEach(h => World.Lighting.Hulls.Add(h));
				}
			}

			var trees = TreesInChunkX(chunk.X);
			foreach (float x in trees)
			{
				var height = HeightMap(x);
				if (chunk.ContainsHeight(height)) // Place a tree if on surface
				{
					var ent = World.CreateEntity("tree");
					ent.GetComponent<BodyComponent>().Position =
						new Vector2(x,
									height);
					chunk.Entities.Add(ent);
				}
			}

			if (chunk.X == 0 && chunk.ContainsHeight(HeightMap(1)))
			{
				var totem = World.CreateEntity("totem");
				totem.GetComponent<BodyComponent>().Position =
						 new Vector2(1, HeightMap(1));
				chunk.Entities.Add(totem);
			}

			chunk.State = ChunkStateEnum.Placed;
		}

		/// <summary>
		/// Remove a chunk and its contents from the game world
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		public void UnplaceChunk(Chunk chunk)
		{
			if (chunk.State != ChunkStateEnum.Placed)
				return;

			lock (World.Physics)
			{
				World.Physics.Remove(chunk.Body);
			}

			chunk.Entities.ForEach(t => t.Remove = true);
			chunk.Hulls.ForEach(h => World.Lighting.Hulls.Remove(h));

			chunk.Body = null;
			chunk.State = ChunkStateEnum.Generated;
		}

		public override void Serialize(BinaryWriter writer)
		{
			writer.Write(Seed);
			List<Chunk> toSave = new List<Chunk>(ChunkCache.Count);
			foreach (KeyValuePair<ulong, Chunk> pair in ChunkCache)
			{
				if (pair.Value.ShouldSave)
					toSave.Add(pair.Value);
			}
			writer.Write(toSave.Count);
			toSave.ForEach(c =>
			{
				writer.Write(c.ID);
				writer.Write(c.Damage.Count);
				c.Damage.ForEach(d => writer.Write(d));
			});
		}

		public override void Deserialize(BinaryReader reader)
		{
			Seed = reader.ReadInt64();
			int cCount = reader.ReadInt32();
			for (int i = 0; i < cCount; ++i)
			{
				ulong id = reader.ReadUInt64();
				Chunk chunk = new Chunk(id);
				int dmgCount = reader.ReadInt32();
				for (int x = 0; x < dmgCount; ++x)
				{
					chunk.Damage.Add(reader.ReadPolygon());
				}
				ChunkCache.Add(id, chunk);
			}
		}

		public static class TerrainHelper
		{
			/// <summary>
			/// Create chunk ID from coordinates
			/// </summary>
			/// <returns>Chunk ID.</returns>
			/// <param name="x">The x coordinate.</param>
			/// <param name="y">The y coordinate.</param>
			public static ulong PackCoordinates(int x, int y)
			{
				return ((ulong)((uint)x) << 32) | (uint)y;
			}

			/// <summary>
			/// Chunk position from chunk ID.
			/// </summary>
			/// <returns>The coordinates.</returns>
			/// <param name="packed">Chunk ID.</param>
			public static Tuple<int, int> UnpackCoordinates(ulong packed)
			{
				return Tuple.Create((int)(packed >> 32), (int)packed);
			}

			/// <summary>
			/// Convert triangulated polygons to render data.
			/// </summary>
			/// <returns>The render data.</returns>
			/// <param name="triangulated">Triangulated polygons.</param>
			/// <param name="c">Vertex color.</param>
			public static VertexPositionColor[] TriangulatedRenderData(List<Vertices> triangulated, Color c)
			{
				var output = new VertexPositionColor[triangulated.Count * 3];
				int index = 0;
				triangulated.ForEach(triangle => triangle.ForEach(vert =>
																  output[index++] = new VertexPositionColor(new Vector3(vert.X, vert.Y, 0), c)));
				return output;
			}

			/// <summary>
			/// Triangulate a polygon.
			/// </summary>
			/// <returns>The triangulation.</returns>
			/// <param name="polygon">Polygon.</param>
			/// <param name="algo">Algorithm.</param>
			public static List<Vertices> Triangulate(List<IntPoint> polygon, TriangulationAlgorithm algo = TriangulationAlgorithm.Earclip)
			{
				return Physics2D.Common.Decomposition.Triangulate.ConvexPartition(PolygonToVertices(polygon, 1), algo);
			}

			/// <summary>
			/// Triangulate a list of polygons.
			/// </summary>
			/// <returns>The triangulation.</returns>
			/// <param name="polygons">Polygons.</param>
			/// <param name="algo">Algorithm.</param>
			public static List<Vertices> Triangulate(List<List<IntPoint>> polygons, TriangulationAlgorithm algo = TriangulationAlgorithm.Earclip)
			{
				List<Vertices> result = new List<Vertices>();
				polygons.ForEach(p => { result.AddRange(Triangulate(p)); });
				return result;
			}

			/// <summary>
			/// Converts a list of IntPoints to vertices.
			/// </summary>
			/// <returns>The polygon in vertices.</returns>
			/// <param name="polygon">Polygon.</param>
			/// <param name="scale">Scale.</param>
			public static Vertices PolygonToVertices(List<IntPoint> polygon, float scale = 1 / 64f)
			{
				Vector2[] v = new Vector2[polygon.Count];
				int i = 0;
				polygon.ForEach(p => v[i++] = new Vector2(p.X, p.Y) * scale);
				return new Vertices(v);
			}

			/// <summary>
			/// Converts vertices to a list of IntPoints
			/// </summary>
			/// <returns>The polygon as a list of IntPoints.</returns>
			/// <param name="verts">Polygon in Vertices.</param>
			/// <param name="scale">Scale.</param>
			public static List<IntPoint> VerticesToPolygon(Vertices verts, float scale = 1 / 64f)
			{
				List<IntPoint> result = new List<IntPoint>();
				verts.ForEach(v => result.Add(new IntPoint((int)(v.X * scale), (int)(v.Y * scale))));
				return result;
			}
		}

		public class Chunk
		{
			public const int SIZE = 512;

			public ulong ID;
			//public Task GenerationTask;
			public ChunkStateEnum State;
			public bool Recalculate = true,
				IsSurface = false;

			public bool ShouldSave
			{
				get { return Damage.Count > 0; }
			}

			public List<Entity> Entities = new List<Entity>();

			public List<List<IntPoint>> Polygons;
			public List<List<IntPoint>> Cavities;
			public List<List<IntPoint>> Damage;

			public List<GrassPoint> GrassPoints = new List<GrassPoint>();

			// Calculated by the Calculate method
			public List<Vertices> PhysicsOutput
			{
				get;
				private set;
			}

			// Calculated by the Calculate method
			public VertexPositionColor[] TriangulatedBackgroundVertices
			{
				get;
				private set;
			}

			// Calculated by the Calculate method
			public VertexPositionColor[] TriangulatedForegroundVertices
			{
				get;
				private set;
			}

			public Body Body;
			public List<Penumbra.Hull> Hulls = new List<Penumbra.Hull>();

			public long Left
			{
				get { return X * SIZE; }
			}

			public long Right
			{
				get { return Left + SIZE; }
			}

			public long Top
			{
				get { return Y * SIZE; }
			}

			public long Bottom
			{
				get { return Top + SIZE; }
			}

			public int X
			{
				get { return TerrainHelper.UnpackCoordinates(ID).Item1; }
			}

			public int Y
			{
				get { return TerrainHelper.UnpackCoordinates(ID).Item2; }
			}

			public Rectangle BoundingBox
			{
				get { return new Rectangle((int)Left, (int)Top, SIZE, SIZE); }
			}

			public Chunk(int x, int y) :
				this(TerrainHelper.PackCoordinates(x, y))
			{

			}

			public Chunk(ulong id)
			{
				ID = id;
				State = ChunkStateEnum.Emtpy;
				PhysicsOutput = new List<Vertices>();
				Damage = new List<List<IntPoint>>();
			}

			/// <summary>
			/// Calculate render and physics data.
			/// </summary>
			public void Calculate(GameWorld world)
			{
				var clipper = new Clipper();
				List<List<IntPoint>> output = new List<List<IntPoint>>();

				// Calculate difference
				clipper.AddPolygons(Polygons, PolyType.ptSubject);
				clipper.AddPolygons(Cavities, PolyType.ptClip);
				clipper.AddPolygons(Damage, PolyType.ptClip);
				clipper.Execute(ClipType.ctDifference, output, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

				Hulls.Clear();
				output.ForEach(o =>
				{
					Vector2[] points = new Vector2[o.Count];
					int index = 0;
					o.ForEach(p => points[index++] = new Vector2(p.X, p.Y));
					Hulls.Add(new Penumbra.Hull(points));
				});

				// Triangulate background for rendering
				{
					var triangulation = TerrainHelper.Triangulate(Polygons);
					TriangulatedBackgroundVertices = new VertexPositionColor[triangulation.Count * 3];
					int index = 0;
					for (int i = 0; i < triangulation.Count; ++i)
						for (int b = 0; b < 3; ++b)
							TriangulatedBackgroundVertices[index++] = new VertexPositionColor(
								new Vector3(triangulation[i][b].X, triangulation[i][b].Y, 0), world.Planet.BackgroundSoilTint);
				}

				// Triangulate foreground for rendering
				{
					var triangulation = TerrainHelper.Triangulate(output);
					TriangulatedForegroundVertices = new VertexPositionColor[triangulation.Count * 3];
					int index = 0;
					for (int i = 0; i < triangulation.Count; ++i)
						for (int b = 0; b < 3; ++b)
							TriangulatedForegroundVertices[index++] = new VertexPositionColor(
								new Vector3(triangulation[i][b].X, triangulation[i][b].Y, 0), world.Planet.SoilTint);
				}

				// Convert to Farseer data
				PhysicsOutput.Clear();
				output.ForEach(p =>
				{
					PhysicsOutput.Add(TerrainHelper.PolygonToVertices(p));
				});
			}

			public bool ContainsHeight(float height)
			{
				return height >= Top && height < Bottom;
			}

			public struct GrassPoint
			{
				public Vector2 Position;
				public float Rotation;
			}
		}

		public enum ChunkStateEnum
		{
			Emtpy,
			Generated,
			Placed
		}
	}
}
