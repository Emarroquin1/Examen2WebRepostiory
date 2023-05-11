using examenWeb2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using static Azure.Core.HttpHeader;

namespace examenWeb2.Controllers
{
   

    public class HomeController : Controller
    {
        NorthwindContext context = new NorthwindContext();

      

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }


        

        public IActionResult Index()
        {
            //listar el total por numero de orden punto 1
            var queryDetalleOrden = from detalle in context.OrderDetails
                                    where detalle.OrderId == 10248
                                    join Product in context.Products on detalle.ProductId equals Product.ProductId
                                    select new
                                    {
                                        detalle.OrderId,
                                        Product.ProductName,
                                        detalle.UnitPrice,
                                        detalle.Quantity,
                                        Total = detalle.UnitPrice * detalle.Quantity
                                    };




            ViewBag.DetalleOrden = queryDetalleOrden;
            ViewBag.orden = 10248;
            //punto 2
            var orden1 = from order in context.Orders
                        where order.ShipCountry == "UK"
                        join Ship in context.Shippers on order.ShipVia equals Ship.ShipperId
                        select new
                        {
                            order.OrderId,
                            order.ShipCountry,
                            Ship.CompanyName

                        };
            ViewBag.orden1 = orden1;

            //punto 3

            var productoMenosVendido = (from orderDetail in context.OrderDetails
                                        join order in context.Orders on orderDetail.OrderId equals order.OrderId
                                        join product in context.Products on orderDetail.ProductId equals product.ProductId
                                        group orderDetail by product into g
                                        orderby g.Sum(od => od.Quantity)
                                        select new
                                        {
                                            NombreProducto = g.Key.ProductName,
                                            CantidadVendida = g.Sum(od => od.Quantity)
                                        }).FirstOrDefault();


            if (productoMenosVendido != null)
            {

              ViewBag.productoMenosVendidoNombre= productoMenosVendido.NombreProducto;
              ViewBag.productoMenosVendidoCantidad= productoMenosVendido.CantidadVendida;
            }


            //Punto 4 

            //transacciones explicitas con rollback

            //otra forma de utilizar el contexto con using



                using (var transaccion = context.Database.BeginTransaction())
                {
                    try
                    {
                    var productosMasVendidos = context.OrderDetails
                                        .GroupBy(od => od.ProductId)
                                        .Select(g => new {
                                            ProductId = g.Key,
                                            CantidadVendida = g.Sum(od => od.Quantity)
                                        })
                                        .OrderBy(p => p.CantidadVendida)
                                        .Take(10)
                                        .Join(context.Products,
                                            p => p.ProductId,
                                            prod => prod.ProductId,
                                            (p, prod) => new {
                                                prod.ProductName,
                                                p.CantidadVendida
                                            })
                                        .ToList();

                     var empleadosColumbia = context.Employees
                    .Where(emp => emp.Territories.Any(t => t.TerritoryDescription == "Columbia"))
                    .Where(emp => DateTime.Now.Year - emp.HireDate.Value.Year > 10)
                    .ToList();

                    ViewBag.ProductosMasVendidos = productosMasVendidos;
                    ViewBag.EmpleadosColumbia = empleadosColumbia;


                    transaccion.Commit();

                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();

                        ViewBag.error = "No se puede realizar la consulta";
                    }
                }
            /*
         5. Listar el nombre del producto, nombre del proveedor y precio de todos los productos que tengan un precio de venta entre $50 y $100.00 y que si haya existencia en órdenes de compras.
         */

            var Producto = from prod in context.Products
                           join prov in context.Suppliers on prod.SupplierId equals prov.SupplierId
                           join orderd in context.OrderDetails on prod.ProductId equals orderd.ProductId
                           where prod.UnitPrice >= 50 && prod.UnitPrice <= 100 && orderd.Quantity > 0
                           select new
                           {
                               prod.ProductName,
                               prov.CompanyName,
                               prod.UnitPrice,
                               orderd.Quantity
                           };
            ViewBag.Producto = Producto;



            /*punto 6 (1.5 pt) Crear una consulta explicita que permita:
            o Agregar un nuevo Shipper
            o Con ese nuevo Shipper agregado, vamos a modificar los registros creados por
            el EmployeeID = 4 en la tabla Orders, y en el campo ShipperVia vamos a
            modificar el valor que tiene, y asignaremos el del ShipperId agregado en el
            punto anterior.
            o No olvide el manejo de errores(use SavePoint)*/
            using (var context = new NorthwindContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        // Agregar un nuevo Shipper
                        Shipper newShipper = new Shipper
                        {
                            CompanyName = "Nuevo Shipper",
                            Phone = "(503) 555-9931"
                        };
                        context.Shippers.Add(newShipper);
                        context.SaveChanges();

                        // Obtener el ShipperId del nuevo Shipper agregado
                        int newShipperId = newShipper.ShipperId;

                        // Modificar los registros de Orders del EmployeeID = 4
                        var ordersToModify = context.Orders.Where(o => o.EmployeeId == 4).ToList();
                        List<string> modifiedOrderDetails = new List<string>();

                        foreach (var order in ordersToModify)
                        {
                            try
                            {
                                // Modificar el valor de ShipperVia con el ShipperId agregado
                                order.ShipVia = newShipperId;
                                context.SaveChanges();

                                // Guardar los detalles de la orden modificada en una lista
                                modifiedOrderDetails.Add($"Order ID: {order.OrderId}, Customer ID: {order.CustomerId}, Ship Via: {order.ShipVia}");
                            }
                            catch (Exception ex)
                            {
                                // Rollback la transacción principal en caso de error
                                transaction.Rollback();

                                // Manejar el error
                                ViewBag.Error = "No se puede modificar el campo ShipperVia en la tabla Orders";
                            }
                        }

                        // Commitear la transacción principal si no hubo errores
                        transaction.Commit();

                        // Agregar el nuevo ShipperId y los detalles de las órdenes modificadas en ViewBag
                        ViewBag.NewShipperId = newShipperId;
                        ViewBag.ModifiedOrders = modifiedOrderDetails;
                    }
                    catch (Exception ex)
                    {
                        // Realizar acciones en caso de error
                        transaction.Rollback();

                        // Manejar el error 
                        ViewBag.Error = "No se puede realizar la transacción";
                    }
                }
            }
            //punto 7
            using (var contexto = new NorthwindContext())
            {
                using (var transaccion = contexto.Database.BeginTransaction())
                {
                    try
                    {
                        var ubi = from order in contexto.Orders
                                  where order.ShipCity == "London" && order.CustomerId == "ALFKI"
                                  join customer in contexto.Customers
                                  on order.CustomerId equals customer.CustomerId
                                  select new
                                  {
                                      order.OrderId,
                                      order.CustomerId,
                                      order.ShipCity,
                                      customer.CompanyName
                                  };
                        List<Object> datos = new List<Object>();
                        foreach (var item in ubi)
                        {
                            datos.Add(item);
                        }
                        ViewBag.datos = datos;



                    }
                    catch (Exception ex)
                    {
                        ViewBag.error = "No se pudo realizar la consulta";
                    }
                }
            }


            return View();


        }

        public IActionResult Privacy()
        {
            return View();

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}