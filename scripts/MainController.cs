using Godot;
using System;
using System.Collections.Generic;

public partial class MainController : Control
{
	private ParserLL1 parser;
	private int passoCount = 0;

	// UI Nodes
	[Export] private LineEdit txtEntrada;
	[Export] private Button btnGerarSentenca;
	[Export] private Button btnIniciar;
	[Export] private Button btnProximoPasso;
	[Export] private Button btnReiniciar;
	
	[Export] private VBoxContainer tablePilha;
	[Export] private Label lblResultado;

	private Node linhaPassoTemplate;
	private GridContainer gridM;

	private Dictionary<string, string> termColMap = new Dictionary<string, string> {
		{"a", "1"}, {"b", "2"}, {"c", "3"}, {"d", "4"}, {"$", "5"}
	};

	public override void _Ready()
	{
		parser = new ParserLL1();
		
		if (btnProximoPasso != null) btnProximoPasso.Disabled = true;
		
		linhaPassoTemplate = tablePilha?.GetNodeOrNull("linhaPasso");
		if (linhaPassoTemplate != null) linhaPassoTemplate.Call("hide");

		gridM = GetNodeOrNull<GridContainer>("ScrollContainer/VBoxContainer/PanelGramatica/HBoxInfo/VBoxTabela/GridM");
		
		ResetarVisualizacao();

		// DEBUG:
		List<string> nulos = new List<string>();
		if (tablePilha == null) nulos.Add("tablePilha");
		if (gridM == null) nulos.Add("gridM");
		if (txtEntrada == null) nulos.Add("txtEntrada");
		if (btnGerarSentenca == null) nulos.Add("btnGerarSentenca");
		if (linhaPassoTemplate == null) nulos.Add("linhaPassoTemplate");
		
		if (nulos.Count > 0 && lblResultado != null)
		{
			lblResultado.Text = "NULLS: " + string.Join(", ", nulos);
		}
	}

	private void ConfigurarTabelaVBox()
	{
		// Headers are now set in the UI directly
	}

	public void OnBtnGerarSentencaPressed()
	{
		if (txtEntrada != null)
		{
			txtEntrada.Text = parser.GenerateRandomSentence();
		}
		ResetarVisualizacao();
	}

	public void OnBtnIniciarPressed()
	{
		string entrada = txtEntrada.Text.Trim();
		if (string.IsNullOrEmpty(entrada))
		{
			lblResultado.Text = "Por favor, digite ou gere uma sentença.";
			lblResultado.AddThemeColorOverride("font_color", new Color(1, 1, 0)); // Amarelo
			return;
		}

		parser.LoadSentence(entrada);
		
		btnProximoPasso.Disabled = false;
		btnIniciar.Disabled = true;
		btnGerarSentenca.Disabled = true;
		txtEntrada.Editable = false;

		lblResultado.Text = "Analisando...";
		lblResultado.AddThemeColorOverride("font_color", new Color(1, 1, 1)); // Branco
		
		ResetarVisualizacao();
		passoCount = 0;
		AdicionarPassoNaTabela("Iniciou a análise.");
	}

	public void OnBtnProximoPassoPressed()
	{
		if (parser.IsFinished) return;

		passoCount++;
		string acao = parser.Step();
		AdicionarPassoNaTabela(acao);

		if (parser.IsFinished)
		{
			btnProximoPasso.Disabled = true;
			if (parser.IsAccepted)
			{
				lblResultado.Text = $"Sentença aceita em {passoCount} passos!";
				lblResultado.AddThemeColorOverride("font_color", new Color(0, 1, 0)); // Verde
			}
			else
			{
				lblResultado.Text = $"Erro de Sintaxe: Erro em {passoCount} passos!";
				lblResultado.AddThemeColorOverride("font_color", new Color(1, 0, 0)); // Vermelho
			}
		}
	}

	public void OnBtnReiniciarPressed()
	{
		btnIniciar.Disabled = false;
		btnGerarSentenca.Disabled = false;
		txtEntrada.Editable = true;
		btnProximoPasso.Disabled = true;
		
		txtEntrada.Text = "";
		lblResultado.AddThemeColorOverride("font_color", new Color(1, 1, 1)); // Branco
		ResetarVisualizacao();
	}

	private void ResetarVisualizacao()
	{
		if (lblResultado != null) lblResultado.Text = "";
		
		if (tablePilha != null)
		{
			foreach (Node child in tablePilha.GetChildren())
			{
				if (child.Name != "LinhaTopo" && child != linhaPassoTemplate && !child.Name.ToString().Contains("Separator"))
				{
					child.QueueFree();
				}
			}
		}

		if (gridM != null)
		{
			foreach (Node child in gridM.GetChildren())
			{
				if (child is Label lbl)
				{
					lbl.RemoveThemeStyleboxOverride("normal");
				}
			}
		}
	}

	private void AdicionarPassoNaTabela(string ultimaAcao)
	{
		if (tablePilha == null) return;
		
		string strPilha = string.Join("", parser.Stack.ToArray());
		char[] arrPilha = strPilha.ToCharArray();
		Array.Reverse(arrPilha);
		strPilha = new string(arrPilha);

		string strFita = parser.GetBufferString();
		strFita = string.Join(" ", strFita.ToCharArray());

		PackedScene linhaScene = GD.Load<PackedScene>("res://scenes/linha_passo.tscn");
		Node novaLinha = linhaScene.Instantiate();
		
		var lblPilha = novaLinha.GetNodeOrNull<Label>("Coluna/estadoPinha");
		if (lblPilha != null) lblPilha.Text = strPilha;
		
		var lblEntrada = novaLinha.GetNodeOrNull<Label>("Coluna2/EstadoEntrada");
		if (lblEntrada != null) lblEntrada.Text = strFita;
		
		var lblAcaoNode = novaLinha.GetNodeOrNull<Label>("Coluna3/estadoAcao");
		if (lblAcaoNode != null) lblAcaoNode.Text = ultimaAcao;
		
		tablePilha.AddChild(novaLinha);

		// Destaca a tabela M
		if (gridM != null)
		{
			// Remove destaque anterior
			foreach (Node child in gridM.GetChildren())
			{
				if (child is Label lbl) lbl.RemoveThemeStyleboxOverride("normal");
			}

			if (parser.LastAppliedRule.HasValue)
			{
				var rule = parser.LastAppliedRule.Value;
				if (termColMap.TryGetValue(rule.a, out string colIndex))
				{
					string labelName = rule.X + colIndex;
					var highlightLabel = gridM.GetNodeOrNull<Label>(labelName);
					if (highlightLabel != null)
					{
						var style = new StyleBoxFlat();
						style.BgColor = new Color(0, 0.4f, 0, 1); // Verde escuro
						highlightLabel.AddThemeStyleboxOverride("normal", style);
					}
				}
			}
		}
	}
}
