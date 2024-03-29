﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Proyecto1.Entidades
{
    internal class Gestor : ISujeto
    {
        private string DatosReservasTurnos; //Datos de reservas de los turnos confirmados y pendientes de confirmación
        private string CorreoRRT;
        private string EstadoRT;
        private DateTime FechaActual;
        private string RTDisponible; // RT disponible de RRT
        private string TurnosConfYPend; //
        private string UsuarioLogueado;
        private Usuario usu;
        private Sesión ses;
        private AsignacionResTecnicoRT art;
        private RecursoTecnológico RT;
        private Pantalla pan;
        private IObservadorMantenimientoCorrectivo suscriptores;
        private string[] contactos;
        private string[] ids;
        private string[] fechas;

        public Gestor()
        {
            usu = new Usuario();
            ses = new Sesión();
            art = new AsignacionResTecnicoRT();
            RT = new RecursoTecnológico();
            pan = new Pantalla(usu);
        }

        public void SetDatosCientificos(string[] Contactos, string[] IDs, string[] Fechas)
        {
            this.contactos = Contactos;
            this.ids = IDs;
            this.fechas = Fechas;
        }

        public string DatosDeReservasDeTurnos
        {
            get => DatosReservasTurnos;
            set => DatosReservasTurnos = value;
        }


        public string CorreoDeRRT
        {
            get => CorreoRRT;
            set => CorreoRRT = value;
        }

        public string EstadoDeRT
        {
            get => EstadoRT;
            set => EstadoRT = value;
        }

        public DateTime LaFechaActual
        {
            get => FechaActual;
            set => FechaActual = value;
        }

        public string LosRTDisponibles
        {
            get => RTDisponible;
            set => RTDisponible = value;
        }

        public string LosTurnosConfYPend
        {
            get => TurnosConfYPend;
            set => TurnosConfYPend = value;
        }

        public string ElUsuarioLogueado
        {
            get => UsuarioLogueado;
            set => UsuarioLogueado = value;
        }


        //el gestor debe llamar a la clase asignacionResponsableTecnologico para que esta obtenga los datos de
        //los RT que ese cientifico tiene disponible, para ello utilizara la "tabla2" creada en el metodo anterior
        public DataTable ObtenerRecursosTecnologicosDisponibles(string nombre)
        {

            DataTable tablaRT = art.MostrarRT(ObtenerRTDisponiblesDeRRT(ObtenerUsuarioLogueado(nombre)), RT);
            if (tablaRT.Rows.Count > 0)
            {
                MessageBox.Show("Recursos encontrados con exito");
                pan.dataGridViewRT.Visible = true; 
            }
            else
            {
                MessageBox.Show("No se encontro ningun recurso");
            }
            return tablaRT;
        }

        //Obtener legajo de usuario a traves de la clase usuario
        //se pasa el nombre por parametro y se envia el mismo la clase sesion
        public string ObtenerUsuarioLogueado(string nombre)
        {
            string nombreUsuario = usu.ObtenerCientifico(nombre);//ses.MostrarCientificoLogueado(nombre);
            return nombreUsuario;
        }

        //Busca si el cientifico actual es responsable de algun RT
        //el gestor obtendra una lista con todos los recursos tecnologicos de los que el cientifico es o fue responsable
        //esa lista sera enviada a la clase asignacion responsable tecnico que se encargara de ver si el cientifico es
        //responsable actual de ese RT, creando otra lista con todos los RT de los que el cientifico es responsable actual
        public DataTable ObtenerRTDisponiblesDeRRT(string nombre)
        {
            DataTable tabla2 = new DataTable();
            tabla2.Columns.Add("nroRT", typeof(string));
            string cadenaConex = System.Configuration.ConfigurationManager.AppSettings["CadenaBD"];
            SqlConnection cn = new SqlConnection(cadenaConex);
            try
            {
                SqlCommand cmd = new SqlCommand();

                string consulta = "SELECT nroRT FROM AsignacionRespTecnRT WHERE legRT LIKE '" + nombre + "'";

                cmd.Parameters.Clear();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = consulta;

                cn.Open();
                cmd.Connection = cn;

                DataTable tabla = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);

                da.Fill(tabla);


                foreach (DataRow row in tabla.Rows)
                {
                    tabla2 = art.EsUsuarioVigente(row["nroRT"].ToString(), tabla2);
                }

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                cn.Close();
            }
            return tabla2;
       }


        //Metodo llamado por la pantalla para obtener los turnos que haya que cancelar, llama a metodo de recurso tecnologico
        //El metodo obtener turnos obtendra los turnos que se cancelaran al aceptar el mantenimiento
        public DataTable BuscarTurnosConfirmadosYPendientesDeConfirmacion(string nrort, string fechaFin, Gestor ges)
        {
            DataTable tablaTurnos = RT.ObtenerTurnos(nrort, fechaFin);

            //ObtenerDatosReserva(tablaTurnos);

            //pan.MostrarReservasDeTurnos(tablaTurnos, ges);
            DataTable TablaDatosTurnos = ObtenerDatosReserva(tablaTurnos);
            return TablaDatosTurnos;
        }

        //metodo llamado por el metodo BuscarTurnosConfirmadosYPendientesDeConfirmacion del mismo gestor para obtener los datos de los 
        //turnos a cancelar.
        //El metodo creara una tabla que sera llenada a traves de un ciclo en cada turno con los datos a mostrar en grilla
        //primero llamara al metodo ObtenerDatosReserva del RT
        public DataTable ObtenerDatosReserva(DataTable tablaTurnos)
        {
            DataTable TablaDatosTurnos = new DataTable("TablaDatosTurnos");
            TablaDatosTurnos.Columns.Add("fechaHoraInicio");
            TablaDatosTurnos.Columns.Add("Nombre");
            TablaDatosTurnos.Columns.Add("Apellido");
            TablaDatosTurnos.Columns.Add("Correo");
            TablaDatosTurnos.Columns.Add("id");
            foreach (DataRow row in tablaTurnos.Rows)
            {
                TablaDatosTurnos = RT.ObtenerDatosReserva(row["id"].ToString(), TablaDatosTurnos);

            }
            return TablaDatosTurnos;

        }

        public void CrearMantenimiento(string nroRT, string fechaFIN, string motivo, string[] fechas,string[] contactos, string[] ids)
        {
            SetDatosCientificos(contactos, ids, fechas);
            RT.CrearMantenimiento(nroRT, fechaFIN, motivo);
            InterfazMail interfazMail = new InterfazMail();
            Suscribir(interfazMail);
            BuscarEstadoActual(nroRT, RT);
            Notificar();
        }

        public void Suscribir(IObservadorMantenimientoCorrectivo observador)
        {
            this.suscriptores = observador;
        }

        public void Notificar()
        {
            
            for(int i = 0; i < contactos.Length; i++)
            {
                
                suscriptores.EnviarNotificacion(fechas[i], contactos[i], ids[i]);
            }
        }

        public void Quitar(IObservadorMantenimientoCorrectivo observador) 
        {
            throw new NotImplementedException();
        }
        //Metodo llamado por el gestor que actualizara los estados, llamara al metodo de RT
        public void BuscarEstadoActual(string nroRT, RecursoTecnológico RT)
        {
            RT.ObtenerEstadoActual(nroRT);
        }

        //Metodo llamado por la pantalla para cancelar los turnos mostrados en grilla
        public void CancelarTurnos(string id)
        {
            RT = new RecursoTecnológico();
            RT.CancelarTurno(id); //Cancela turnos anteriores a la fecha actual
        }
    }
}
