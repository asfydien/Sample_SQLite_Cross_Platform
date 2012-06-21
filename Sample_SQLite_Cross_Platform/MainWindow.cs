using System;
using Gtk;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;

public partial class MainWindow: Gtk.Window
{	
	SQLiteConnection SQLiteKoneksi;
	SQLiteCommand SQLitePerintah;

	string strPathDB = "";
	string strPathKamus = "";

	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}
	protected void OnButton1Clicked (object sender, EventArgs e)
	{
		strPathDB = startupPath () + @"/kamus.db";
		strPathKamus = startupPath () + @"/gkamus-id.dict";

		if (File.Exists (strPathKamus)) {

			// jalankan di thread terpisah
			Thread tr = new Thread (transaksi);
			tr.Start ();    

		}
		else{
			MessageDialog md = new MessageDialog (this, DialogFlags.Modal, MessageType.Info,
			                                      ButtonsType.Ok, "gkamus-id.dict?");
			int result = md.Run ();
			md.Destroy();
		}

	}	

	protected void OnButton2Clicked (object sender, EventArgs e)
	{
		Application.Quit(); 
	}

	void transaksi()
	{
		SQLiteKoneksi = new SQLiteConnection("Data Source=" + strPathDB);
		string[] dataKamus = File.ReadAllLines(strPathKamus);
		string[] strPecah;
		
		SQLiteKoneksi.Open();
		
		// mulai transaksi
		using (SQLiteTransaction SQLiteTransaksi = SQLiteKoneksi.BeginTransaction())
		{
			using (SQLitePerintah = new SQLiteCommand(SQLiteKoneksi))
			{
				// buat database jika belum ada
				SQLitePerintah.CommandText = "CREATE TABLE IF NOT EXISTS kamus (kata char(50), arti char(150))";
				SQLitePerintah.ExecuteNonQuery();
				
				// query untuk masukan data
				SQLitePerintah.CommandText = "INSERT OR REPLACE INTO kamus (kata, arti) " +
											 "VALUES (@kata, @arti)";  
				
				// tambahkan parameter
				SQLitePerintah.Parameters.Add("kata", DbType.String); 
				SQLitePerintah.Parameters.Add("arti", DbType.String);
				
				// masukan semua kata & arti
				for (int i=6; i<dataKamus.Length; i++ ){
					strPecah = dataKamus[i].Split('	');

					SQLitePerintah.Parameters[0].Value  =  strPecah[0];
					SQLitePerintah.Parameters[1].Value  =  strPecah[1];
					
					SQLitePerintah.ExecuteNonQuery(); 
					
					// update progress
					Gtk.Application.Invoke( delegate {
						double psn = (i*100 / dataKamus.Length);
						progressbar1.Text = psn.ToString() + "%";
						progressbar1.Fraction = psn /100; 
					});
				}
			}
			
			// akhiri transaksi
			SQLiteTransaksi.Commit(); 
		}
		
		SQLiteKoneksi.Close();
	}
	
	string startupPath()
	{
		string ret = System.Reflection.Assembly.GetExecutingAssembly().Location; 
		return System.IO.Path.GetDirectoryName(ret);    
	}

}
