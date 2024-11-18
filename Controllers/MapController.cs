using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SQLite;


namespace PotholeDetectionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {
        private readonly string _mbtilesPath = "D://map/VNU.mbtiles";

        [HttpGet("{z}/{x}/{y}")]
        public IActionResult GetTile(int z, int x, int y)
        {
            using (var connection = new SQLiteConnection($"Data Source={_mbtilesPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT tile_data FROM tiles WHERE zoom_level = @z AND tile_column = @x AND tile_row = @y", connection))
                {
                    command.Parameters.AddWithValue("@z", z);
                    command.Parameters.AddWithValue("@x", x);
                    command.Parameters.AddWithValue("@y", y);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            byte[] tileData = (byte[])reader["tile_data"];
                            return File(tileData, "image/png");
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
            }
        }

        [HttpGet("get-info")]
        public IActionResult GetTileInfo()
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={_mbtilesPath};Version=3;"))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT zoom_level, tile_column, tile_row FROM tiles", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        var tilesInfo = new List<object>();
                        while (reader.Read())
                        {
                            tilesInfo.Add(new
                            {
                                ZoomLevel = reader.GetInt32(0),
                                TileColumn = reader.GetInt32(1),
                                TileRow = reader.GetInt32(2)
                            });
                        }
                        return Ok(new { tilesInfo });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet("get-zoom14-tiles-map")]
        public IActionResult GetZoom14TilesMap()
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={_mbtilesPath};Version=3;"))
                {
                    connection.Open();
                    // Query để lấy tile_data từ cơ sở dữ liệu MBTiles
                    using (var command = new SQLiteCommand("SELECT tile_column, tile_row, tile_data FROM tiles WHERE zoom_level = 14", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        // Tạo một danh sách để chứa các tile
                        List<byte[]> tilesData = new List<byte[]>();

                        while (reader.Read())
                        {
                            int tileColumn = reader.GetInt32(0);
                            int tileRow = reader.GetInt32(1);
                            byte[] tileData = (byte[])reader["tile_data"];

                            // Thêm tile_data vào danh sách
                            tilesData.Add(tileData);
                        }

                        // Nếu có tiles thì trả về file đầu tiên từ danh sách
                        if (tilesData.Count > 0)
                        {
                            return File(tilesData[0], "image/png");
                        }
                        else
                        {
                            return NotFound("No tiles found for zoom level 14.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }


    }
}
