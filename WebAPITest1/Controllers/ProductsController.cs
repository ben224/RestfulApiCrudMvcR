using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Web.Http;
using WebAPITest1.Models;

namespace WebAPITest1.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["BenTempDb"].ConnectionString;

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            var list = new List<Product>();
            try
            {
                using (var conn = new SqlConnection(_connStr))
                using (var cmd = new SqlCommand("SELECT ProductId, Name, Price, Stock FROM Product", conn))
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new Product
                            {
                                ProductId = rdr.GetInt32(rdr.GetOrdinal("ProductId")),
                                Name = rdr["Name"] as string,
                                Price = rdr.GetInt32(rdr.GetOrdinal("Price")),
                                Stock = rdr.GetInt32(rdr.GetOrdinal("Stock"))
                            });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                Product p = null;
                using (var conn = new SqlConnection(_connStr))
                using (var cmd = new SqlCommand("SELECT ProductId, Name, Price, Stock FROM Product WHERE ProductId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            p = new Product
                            {
                                ProductId = rdr.GetInt32(rdr.GetOrdinal("ProductId")),
                                Name = rdr["Name"] as string,
                                Price = rdr.GetInt32(rdr.GetOrdinal("Price")),
                                Stock = rdr.GetInt32(rdr.GetOrdinal("Stock"))
                            };
                        }
                    }
                }
                if (p == null) return NotFound();
                return Ok(p);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Create([FromBody] Product product)
        {
            if (product == null) return BadRequest("Product body required.");
            if (product.ProductId <= 0) return BadRequest("ProductId 必須為大於 0 的整數。");

            try
            {
                using (var conn = new SqlConnection(_connStr))
                {
                    conn.Open();

                    // 檢查 ProductId 是否已存在，若存在回傳 409 Conflict
                    using (var checkCmd = new SqlCommand("SELECT COUNT(1) FROM Product WHERE ProductId = @id", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", product.ProductId);
                        var existsObj = checkCmd.ExecuteScalar();
                        var exists = (existsObj != null && existsObj != DBNull.Value) ? Convert.ToInt32(existsObj) : 0;
                        if (exists > 0) return Conflict();
                    }

                    using (var cmd = new SqlCommand("INSERT INTO Product (ProductId, Name, Price, Stock) VALUES (@productid, @name, @price, @stock)", conn))
                    {
                        cmd.Parameters.AddWithValue("@productid", product.ProductId);
                        cmd.Parameters.AddWithValue("@name", (object)product.Name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@price", product.Price);
                        cmd.Parameters.AddWithValue("@stock", product.Stock);
                        var rows = cmd.ExecuteNonQuery();
                        if (rows == 0) return InternalServerError(new Exception("Insert failed."));
                    }
                }

                // 建立正確的 Location URI（確保末端有斜線）
                var baseUri = Request.RequestUri.ToString().TrimEnd('/');
                var location = new Uri(baseUri + "/" + product.ProductId);
                return Created(location, product);
            }
            catch (SqlException sqlEx)
            {
                // 常見原因：資料表 Product 的 ProductId 為 IDENTITY，不允許顯式 INSERT
                return InternalServerError(sqlEx);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] Product product)
        {
            if (product == null) return BadRequest("Product body required.");
            if (id != product.ProductId) return BadRequest("Id mismatch.");

            try
            {
                int rows;
                using (var conn = new SqlConnection(_connStr))
                using (var cmd = new SqlCommand("UPDATE Product SET Name = @name, Price = @price, Stock = @stock WHERE ProductId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@name", (object)product.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@price", product.Price);
                    cmd.Parameters.AddWithValue("@stock", product.Stock);
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    rows = cmd.ExecuteNonQuery();
                }
                if (rows == 0) return NotFound();
                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                int rows;
                using (var conn = new SqlConnection(_connStr))
                using (var cmd = new SqlCommand("DELETE FROM Product WHERE ProductId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    rows = cmd.ExecuteNonQuery();
                }
                if (rows == 0) return NotFound();
                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}