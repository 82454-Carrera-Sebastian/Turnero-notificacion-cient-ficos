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
    public class RecursoTecnológico
    {
        private int NumeroRT;
        private DateTime FechaAlta;
        private int PeriodicidadMantPreventivo;
        private int DuracionMantPreventivo;
        private int FraccionHorarioTurnos;
        private Turno tur;
        private CambioEstadoRT cam;
        private CambioEstadoTurno camT;

        public RecursoTecnológico(int numRT, DateTime fechaAlta)
        {
            NumeroRT = numRT;
            FechaAlta = fechaAlta;
            tur = new Turno();
        }

        public RecursoTecnológico()
        {

        }

        public int NumRecurso
        {
            get => NumeroRT;
            set => NumeroRT = value;
        }

        public DateTime FechaDeAlta
        {
            get => FechaAlta;
            set => FechaAlta = value;
        }

        public int PeriodicidadDeMantPrev
        {
            get => PeriodicidadMantPreventivo;
            set => PeriodicidadMantPreventivo = value;
        }

        public int DuracionDeMantenimientoPrev
        {
            get => DuracionMantPreventivo;
            set => DuracionMantPreventivo = value;
        }

        public int FraccionHorarioDeTurnos
        {
            get => FraccionHorarioTurnos;
            set => FraccionHorarioTurnos = value;
        }


        //LLamado por la clase AsignacionRestecnico para ver si un RT esta en mantenimiento en ese momento
        //le llegara por parametro un RT y utilizando la tabla mantenimiento verificara si este tiene fechaFin o no, si no la posee
        //no estara activo por lo tanto sus datos no se mostraran
        public bool esActivo(string nroRecurso)
        {
            bool esact = true;
            string cadenaConex = System.Configuration.ConfigurationManager.AppSettings["CadenaBD"];
            SqlConnection cn = new SqlConnection(cadenaConex);
            try
            {
                SqlCommand cmd = new SqlCommand();

                string consulta = "SELECT fechaFin FROM Mantenimiento WHERE idRecurso LIKE '" + int.Parse(nroRecurso) + "' AND fechaFin IS NULL";

                cmd.Parameters.Clear();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = consulta;

                cn.Open();
                cmd.Connection = cn;


                SqlDataReader dataReader = cmd.ExecuteReader();
                if (dataReader != null && dataReader.Read())
                {
                    if ((dataReader["fechaFin"].ToString()) == "")
                    {
                        esact = false;
                    }

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
            return esact;
        }


        //Metodo llamado por el gestor para obtener turnos cancelables. El RecursoTecnologico obtendra los turnos disponibles, 
        //los Turnos veran si son cancelables (obteniendo los datos del ultimo estado) y
        //preguntandoles al cambioDeEstadoTurno si es actual (es decir si esta dentro del intervalo de tiempo del mantenimiento) y
        //el actual le pregunta a estado si es cancelable, es decir si estan pendientes o confirmadas
        public DataTable ObtenerTurnos(string nrort, string fechaFin)
        {
            DataTable tabla2 = new DataTable();
            string cadenaConex = System.Configuration.ConfigurationManager.AppSettings["CadenaBD"];
            SqlConnection cn = new SqlConnection(cadenaConex);
            try
            {
                SqlCommand cmd = new SqlCommand();
                string consulta = "SELECT id FROM Turno WHERE idRecurso LIKE '" + nrort + "' AND fechaHoraInicio > GETDATE()";

                cmd.Parameters.Clear();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = consulta;

                cn.Open();
                cmd.Connection = cn;


                //SqlDataReader dataReader = cmd.ExecuteReader();
                DataTable tabla = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);

                da.Fill(tabla);


                foreach (DataRow row in tabla.Rows)
                {
                    tur = new Turno();
                    tabla2 = tur.EsCancelable(row["id"].ToString(), tabla2, fechaFin);
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

        //Metodo llamado por le gestor para obtenrer los datos de los turnos a cancelar
        //este metodo llamara a la clase turno para obtener fechaHoraInicio y el resto de los datos
        public DataTable ObtenerDatosReserva(string id, DataTable TablaDatosTurnos)
        {
            tur = new Turno();
            TablaDatosTurnos = tur.MostrarFechaHora(id, TablaDatosTurnos);
            return TablaDatosTurnos;
        }

        //metodo llamado para el gestor para cargar el mantenimiento 
        //CONVERT(DATETIME," + fechaFIN + ",103)
        public void CrearMantenimiento(string nroRT, string fechaFIN, string motivo)
        {
            string cadenaConex = System.Configuration.ConfigurationManager.AppSettings["CadenaBD"];
            SqlConnection cn = new SqlConnection(cadenaConex);
            SqlTransaction objtransaction = null;
            try
            {
                SqlCommand cmd = new SqlCommand();
                string consulta = "INSERT INTO Mantenimiento (fechaInicio, fechaFin, motivo, idRecurso) VALUES (GETDATE(), CONVERT(datetime,'" + fechaFIN + "',105),'" + motivo + "',"+ nroRT + ")";
                cmd.Parameters.Clear();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = consulta;

                cn.Open();
                objtransaction = cn.BeginTransaction("CrearMantenimiento");
                cmd.Transaction = objtransaction;
                cmd.Connection = cn;
                cmd.ExecuteNonQuery();
                objtransaction.Commit();



            }
            catch (Exception ex)
            {
                objtransaction.Rollback();
            }
            finally
            {
                cn.Close();
            }

        }
        
        //Metodo llamado por el gestor para actualizar el estado
        public void ObtenerEstadoActual(string nroRT)
        {
            cam = new CambioEstadoRT();
            string actual = cam.EsActual(nroRT);
            cam.CambiarEstado(actual);
            cam.CrearEstado(nroRT);

        }

        internal void CancelarTurno(string id)
        {
            camT = new CambioEstadoTurno();
            string actual = camT.EsActualTurno(id);
            camT.SetFechaFin(actual);
            camT.CrearEstado(id);
        }

        
    }

}
