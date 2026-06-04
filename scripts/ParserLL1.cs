using System;
using System.Collections.Generic;

public class ParserLL1
{
	public Stack<string> Stack { get; private set; }
	public string Buffer { get; private set; }
	public int Pointer { get; private set; }
	public bool IsFinished { get; private set; }
	public bool IsAccepted { get; private set; }
	public (string X, string a)? LastAppliedRule { get; private set; }

	private Dictionary<(string, string), string[]> table;

	public ParserLL1()
	{
		Stack = new Stack<string>();
		InitializeTable();
	}

	private void InitializeTable()
	{
		table = new Dictionary<(string, string), string[]>();

		// M[S, a] = S -> aA
		table[("S", "a")] = new string[] { "a", "A" };
		// M[S, c] = S -> cB
		table[("S", "c")] = new string[] { "c", "B" };
		
		// M[A, d] = A -> dA
		table[("A", "d")] = new string[] { "d", "A" };
		// M[A, a] = A -> C
		table[("A", "a")] = new string[] { "C" };
		// M[A, b] = A -> C
		table[("A", "b")] = new string[] { "C" };

		// M[C, a] = C -> a
		table[("C", "a")] = new string[] { "a" };
		// M[C, b] = C -> b
		table[("C", "b")] = new string[] { "b" };

		// M[B, b] = B -> bB
		table[("B", "b")] = new string[] { "b", "B" };
		// M[B, $] = B -> ε
		table[("B", "$")] = new string[] { "ε" };
	}

	public void LoadSentence(string sentence)
	{
		Buffer = sentence + "$";
		Pointer = 0;
		IsFinished = false;
		IsAccepted = false;
		LastAppliedRule = null;
		
		Stack.Clear();
		Stack.Push("$");
		Stack.Push("S");
	}

	// Retorna a representação atual da pilha como string
	public string GetStackString()
	{
		string[] arr = Stack.ToArray();
		Array.Reverse(arr); // A base fica no início do array, topo no final
		return "[" + string.Join(", ", arr) + "]";
	}

	// Retorna a fita de entrada a partir do ponteiro atual
	public string GetBufferString()
	{
		if (Pointer >= Buffer.Length) return "";
		return Buffer.Substring(Pointer);
	}

	public string Step()
	{
		LastAppliedRule = null;
		if (IsFinished) return "Análise já finalizada.";

		if (Stack.Count == 0)
		{
			IsFinished = true;
			return "Erro: Pilha vazia antes do término.";
		}

		string X = Stack.Peek();
		string a = Buffer[Pointer].ToString();

		if (X == "$" && a == "$")
		{
			Stack.Pop();
			IsFinished = true;
			IsAccepted = true;
			return "ACEITO: Símbolo $ final alcançado na pilha e fita.";
		}

		if (IsTerminal(X) || X == "$")
		{
			if (X == a)
			{
				Stack.Pop();
				Pointer++;
				return $"'{X}' confere. Avança fita e desempilha.";
			}
			else
			{
				IsFinished = true;
				IsAccepted = false;
				return $"ERRO SINTÁTICO: Esperava '{X}', encontrou '{a}'.";
			}
		}
		else // Não-terminal
		{
			if (table.TryGetValue((X, a), out string[] production))
			{
				Stack.Pop();
				LastAppliedRule = (X, a);
				
				string productionStr;
				if (production.Length == 1 && production[0] == "ε")
				{
					productionStr = "ε";
				}
				else
				{
					// Empilha na ordem inversa para que o primeiro símbolo da regra fique no topo
					for (int i = production.Length - 1; i >= 0; i--)
					{
						Stack.Push(production[i]);
					}
					productionStr = string.Join("", production);
				}

				return $"{X} -> {productionStr}";
			}
			else
			{
				IsFinished = true;
				IsAccepted = false;
				return $"ERRO SINTÁTICO: Sentença rejeitada.";
			}
		}
	}

	private bool IsTerminal(string symbol)
	{
		return symbol == "a" || symbol == "b" || symbol == "c" || symbol == "d";
	}

	public string GenerateRandomSentence()
	{
		Random rand = new Random();
		List<string> result = new List<string>();
		Stack<string> genStack = new Stack<string>();
		genStack.Push("S");

		while (genStack.Count > 0)
		{
			string top = genStack.Pop();

			if (IsTerminal(top))
			{
				result.Add(top);
			}
			else if (top != "ε" && top != "$")
			{
				List<string[]> possibleProductions = new List<string[]>();
				
				foreach (var kvp in table)
				{
					if (kvp.Key.Item1 == top)
					{
						possibleProductions.Add(kvp.Value);
					}
				}

				if (possibleProductions.Count > 0)
				{
					int choice = rand.Next(possibleProductions.Count);
					string[] prod = possibleProductions[choice];
					
					if (prod.Length != 1 || prod[0] != "ε")
					{
						for (int i = prod.Length - 1; i >= 0; i--)
						{
							genStack.Push(prod[i]);
						}
					}
				}
			}
		}

		return string.Join("", result);
	}
}
