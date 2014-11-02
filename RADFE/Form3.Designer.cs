namespace RADFE
{
    partial class Form3
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.radioButtonMap1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.button1 = new System.Windows.Forms.Button();
            this.checkBox_cycle = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // radioButtonMap1
            // 
            this.radioButtonMap1.AutoSize = true;
            this.radioButtonMap1.Checked = true;
            this.radioButtonMap1.Location = new System.Drawing.Point(46, 46);
            this.radioButtonMap1.Name = "radioButtonMap1";
            this.radioButtonMap1.Size = new System.Drawing.Size(70, 17);
            this.radioButtonMap1.TabIndex = 0;
            this.radioButtonMap1.TabStop = true;
            this.radioButtonMap1.Text = "Test Map";
            this.radioButtonMap1.UseVisualStyleBackColor = true;
            this.radioButtonMap1.CheckedChanged += new System.EventHandler(this.radioButtonMap1_CheckedChanged);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(45, 93);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(71, 17);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Real Map";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(245, 67);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // checkBox_cycle
            // 
            this.checkBox_cycle.AutoSize = true;
            this.checkBox_cycle.Location = new System.Drawing.Point(46, 135);
            this.checkBox_cycle.Name = "checkBox_cycle";
            this.checkBox_cycle.Size = new System.Drawing.Size(52, 17);
            this.checkBox_cycle.TabIndex = 3;
            this.checkBox_cycle.Text = "Cycle";
            this.checkBox_cycle.UseVisualStyleBackColor = true;
            // 
            // Form3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(370, 164);
            this.Controls.Add(this.checkBox_cycle);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.radioButton2);
            this.Controls.Add(this.radioButtonMap1);
            this.Name = "Form3";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form3";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton radioButtonMap1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox_cycle;
    }
}