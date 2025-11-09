using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeSalimos.Shared.Modelos
{
    public class Comercio
    {
        [Key]
        public int ID_Comercio { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Nombre { get; set; }

        public int Capacidad { get; set; }

        public int Mesas { get; set; }

        public string GeneroMusical { get; set; }

        public string TipoDocumento { get; set; }

        public string NroDocumento { get; set; }

        public string Direccion { get; set; }

        [DataType(DataType.EmailAddress)]
        [RegularExpression(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$", ErrorMessage = "El correo es inválido.")]
        public string Correo { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string Telefono { get; set; }

        public TimeSpan? HoraIngreso { get; set; }

        public TimeSpan? HoraCierre { get; set; }
        
        public string? MotivoRechazo { get; set; }

        public byte[]? Foto { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public bool Estado { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int ID_Usuario { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public int ID_TipoComercio { get; set; }

        [ForeignKey("ID_Usuario")]
        public Usuario? Usuario { get; set; }

        [ForeignKey("ID_TipoComercio")]
        public TipoComercio? TipoComercio { get; set; }
    }
}
