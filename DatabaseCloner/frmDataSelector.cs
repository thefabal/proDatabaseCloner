﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace DatabaseCloner {
    public partial class frmDataSelector: Form {
        public DatabaseBackup backup_settings;

        private readonly proGEDIA.Utilities.Database database;
        private readonly string db_name;

        public frmDataSelector( proGEDIA.Utilities.Database database, string db_name, ref DatabaseBackup backup_settings ) {
            this.database = database;
            this.db_name = db_name;
            this.backup_settings = backup_settings;

            InitializeComponent();

            for(int i = 0; i < dgvTableList.Columns.Count; i++ ) {
                dgvTableList.Columns[ i ].HeaderCell.Style.Alignment = DataGridViewContentAlignment.BottomCenter;
            }
            dgvTableList.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgvTableList.AllowUserToResizeRows = false;

            this.MinimumSize = this.Size;
        }

        private void frmDataSelector_Load( object sender, EventArgs e ) {
            /**
             * Tables
            **/
            try {
                List<DatabaseTableEntry> tableList = backup_settings.GetListTable();
                foreach( DatabaseTableEntry tn in tableList ) {
                    dgvTableList.Rows.Add( "table", tn.schema, tn.name, true, true );
                }
            } catch( Exception ex ) {
                MessageBox.Show( ex.Message );
            }

            /**
              * Views
             **/
            try {
                List<DatabaseTableEntry> tableList = backup_settings.GetListView();
                foreach( DatabaseTableEntry tn in tableList ) {
                    dgvTableList.Rows[ dgvTableList.Rows.Add( "view", tn.schema, tn.name, true, false ) ].Cells[ 4 ].ReadOnly = true;
                }
            } catch( Exception ex ) {
                MessageBox.Show( ex.Message );
            }

            /**
             * Functions
            **/
            try {
                List<DatabaseTableEntry> tableList = backup_settings.GetListFunction();
                foreach( DatabaseTableEntry tn in tableList ) {
                    dgvTableList.Rows[ dgvTableList.Rows.Add( "function", tn.schema, tn.name, true, false ) ].Cells[ 4 ].ReadOnly = true;
                }
            } catch( Exception ex ) {
                MessageBox.Show( ex.Message );
            }

            /**
             * Stored Procedures
            **/
            try {
                List<DatabaseTableEntry> tableList = backup_settings.GetListProcedures();
                foreach( DatabaseTableEntry tn in tableList ) {
                    dgvTableList.Rows[ dgvTableList.Rows.Add( "procedure", tn.schema, tn.name, true, false ) ].Cells[ 4 ].ReadOnly = true;
                }
            } catch( Exception ex ) {
                MessageBox.Show( ex.Message );
            }

            /**
             * Database Triggers
            **/
            try {
                List<DatabaseTableEntry> tableList = backup_settings.GetListTrigger();
                foreach( DatabaseTableEntry tn in tableList ) {
                    dgvTableList.Rows[ dgvTableList.Rows.Add( "trigger", tn.schema, tn.name, true, false ) ].Cells[ 4 ].ReadOnly = true;
                }
            } catch( Exception ex ) {
                MessageBox.Show( ex.Message );
            }

            arrangeCheckboxes();
        }

        private void arrangeCheckboxes() {
            if( dgvTableList.Rows.Count == 0 ) {
                cbData.Visible = false;
                cbSchema.Visible = false;
            } else {
                cbData.Visible = true;
                cbSchema.Visible = true;
                cbData.Location = new Point( dgvTableList.GetCellDisplayRectangle( 4, 0, true ).Location.X + dgvTableList.Location.X + dgvTableList.Columns[ 4 ].Width / 2 - cbData.Width / 2, dgvTableList.Location.Y + 5 );
                cbSchema.Location = new Point( dgvTableList.GetCellDisplayRectangle( 3, 0, true ).Location.X + dgvTableList.Location.X + dgvTableList.Columns[ 3 ].Width / 2 - cbSchema.Width / 2, dgvTableList.Location.Y + 5 );
            }
        }

        private void cbSchema_CheckedChanged( object sender, EventArgs e ) {
            dgvTableList.BeginEdit(false);
            foreach( DataGridViewRow row in dgvTableList.Rows ) {
                if( ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).Value == ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).FalseValue || ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).Value == null ) {
                    ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).Value = true;
                } else {
                    ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).Value = null;
                }
            }
            dgvTableList.EndEdit();
        }

        private void cbData_CheckedChanged( object sender, EventArgs e ) {
            dgvTableList.BeginEdit( false );
            foreach( DataGridViewRow row in dgvTableList.Rows ) {
                if( (string)row.Cells[ 0 ].Value == "table" ) {
                    if( ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).Value == ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).FalseValue || ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).Value == null ) {
                        ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).Value = true;
                    } else {
                        ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).Value = null;
                    }
                }
            }
            dgvTableList.EndEdit();
        }

        private void btnOK_Click( object sender, EventArgs e ) {
            backup_settings.backupSettings.Clear();

            foreach( DataGridViewRow row in dgvTableList.Rows ) {
                backup_settings.backupSettings.Add( 
                    new DatabaseBackupSettings( 
                        row.Cells[ 0 ].Value.ToString(), 
                        row.Cells[ 1 ].Value.ToString(), 
                        row.Cells[ 2 ].Value.ToString(),
                        Convert.ToBoolean( row.Cells[ 3 ].Value ) == true,
                        Convert.ToBoolean( row.Cells[ 4 ].Value ) == true
                ) );
            }

            this.DialogResult = DialogResult.OK;
        }

        private void dgvTableList_ColumnWidthChanged( object sender, DataGridViewColumnEventArgs e ) {
            arrangeCheckboxes();
        }
    }
}
