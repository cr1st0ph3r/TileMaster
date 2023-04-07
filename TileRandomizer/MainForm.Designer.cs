namespace TileRandomizer
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lbFiles = new System.Windows.Forms.ListBox();
            this.pbImage = new TileRandomizer.InterpolatedBox();
            this.lblColors = new System.Windows.Forms.Label();
            this.btnRandomize = new System.Windows.Forms.Button();
            this.pbNewImage = new TileRandomizer.InterpolatedBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.lblUnique = new System.Windows.Forms.Label();
            this.btnSaveTileColorData = new System.Windows.Forms.Button();
            this.txtTileId = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbNewImage)).BeginInit();
            this.SuspendLayout();
            // 
            // lbFiles
            // 
            this.lbFiles.FormattingEnabled = true;
            this.lbFiles.ItemHeight = 15;
            this.lbFiles.Location = new System.Drawing.Point(12, 12);
            this.lbFiles.Name = "lbFiles";
            this.lbFiles.Size = new System.Drawing.Size(261, 589);
            this.lbFiles.TabIndex = 0;
            this.lbFiles.SelectedIndexChanged += new System.EventHandler(this.lbFiles_SelectedIndexChanged);
            // 
            // pbImage
            // 
            this.pbImage.Location = new System.Drawing.Point(279, 12);
            this.pbImage.Name = "pbImage";
            this.pbImage.Size = new System.Drawing.Size(250, 250);
            this.pbImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbImage.TabIndex = 1;
            this.pbImage.TabStop = false;
            // 
            // lblColors
            // 
            this.lblColors.AutoSize = true;
            this.lblColors.Location = new System.Drawing.Point(279, 265);
            this.lblColors.Name = "lblColors";
            this.lblColors.Size = new System.Drawing.Size(50, 15);
            this.lblColors.TabIndex = 2;
            this.lblColors.Text = "0 Colors";
            // 
            // btnRandomize
            // 
            this.btnRandomize.Location = new System.Drawing.Point(301, 313);
            this.btnRandomize.Name = "btnRandomize";
            this.btnRandomize.Size = new System.Drawing.Size(120, 32);
            this.btnRandomize.TabIndex = 3;
            this.btnRandomize.Text = "Randomize new Tile";
            this.btnRandomize.UseVisualStyleBackColor = true;
            this.btnRandomize.Click += new System.EventHandler(this.btnRandomize_Click);
            // 
            // pbNewImage
            // 
            this.pbNewImage.Location = new System.Drawing.Point(279, 351);
            this.pbNewImage.Name = "pbNewImage";
            this.pbNewImage.Size = new System.Drawing.Size(250, 250);
            this.pbNewImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbNewImage.TabIndex = 1;
            this.pbNewImage.TabStop = false;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(427, 313);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(66, 32);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save PNG";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // lblUnique
            // 
            this.lblUnique.AutoSize = true;
            this.lblUnique.Location = new System.Drawing.Point(424, 265);
            this.lblUnique.Name = "lblUnique";
            this.lblUnique.Size = new System.Drawing.Size(91, 15);
            this.lblUnique.TabIndex = 2;
            this.lblUnique.Text = "0 Unique Colors";
            // 
            // btnSaveTileColorData
            // 
            this.btnSaveTileColorData.Location = new System.Drawing.Point(427, 283);
            this.btnSaveTileColorData.Name = "btnSaveTileColorData";
            this.btnSaveTileColorData.Size = new System.Drawing.Size(52, 24);
            this.btnSaveTileColorData.TabIndex = 5;
            this.btnSaveTileColorData.Text = "Save";
            this.btnSaveTileColorData.UseVisualStyleBackColor = true;
            this.btnSaveTileColorData.Click += new System.EventHandler(this.btnSaveTileColorData_Click);
            // 
            // txtTileId
            // 
            this.txtTileId.Location = new System.Drawing.Point(320, 283);
            this.txtTileId.Name = "txtTileId";
            this.txtTileId.Size = new System.Drawing.Size(100, 23);
            this.txtTileId.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(279, 287);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Tile Id:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(536, 609);
            this.Controls.Add(this.txtTileId);
            this.Controls.Add(this.btnSaveTileColorData);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnRandomize);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblUnique);
            this.Controls.Add(this.lblColors);
            this.Controls.Add(this.pbNewImage);
            this.Controls.Add(this.pbImage);
            this.Controls.Add(this.lbFiles);
            this.Name = "MainForm";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbNewImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ListBox lbFiles;
        private InterpolatedBox pbImage;
        private Label lblColors;
        private Button btnRandomize;
        private InterpolatedBox pbNewImage;
        private Button btnSave;
        private Label lblUnique;
        private Button btnSaveTileColorData;
        private TextBox txtTileId;
        private Label label1;
    }
}