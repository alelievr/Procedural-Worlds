﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using System.IO;

namespace ProceduralWorlds.Core
{
	//Command line interpreter for BaseGraph
	public static partial class BaseGraphCLI
	{

		/*
		Valid command syntaxes:

		> NewNode nodeType nodeName [position] [attr=...]
		Create a new node of type nodeType in the graph, if the node
		type does not exists, an excetion is raised and the node is
		not created.
		The position is in pixels so it's an integer, floating values
		will not be accepted and will result with a lex error.

		ex:
			> NewNode NodeAdd simple_add
			> NewNode NodeAdd simple_add (10, 100)
			> NewNode PWPerlinNoise2D perlin attr={ frequency: 1.2, octaves: 4, scale: 8.5 }

		---------------------------------------------

		> Link nodeName1 nodeName2
		Create a link between two nodes, this command will try to find
		any linkable anchor between the two nodes, if there is not an
		exception is raised and the link is not created.

		ex:
			> NewNode NodeSlider slider
			> NewNode NodeAdd add
			> Link slider add

		---------------------------------------------

		> LinkAnchor nodeName1:anchorFieldIndex nodeName2:anchorFieldIndex
		> LinkAnchor nodeName1:anchorFieldName nodeName2:anchorFieldName
		Create a link between two nodes using the anchor index / name
		to find the anchor to link.
		when using the anchorFieldName, it'll try to find the field inside
		the nodes using their names and then link them. If the specified anchor
		is multiple (PWArray<>) the link will be created on the first unlinked
		available anchor.
		Remarks: anchorFieldIndex start from 0.

		ex:
			> NewNode NodeSlider slider
			> NewNode NodeAdd add
			> LinkAnchor slider:outpuValue add:values
			> LinkAnchor slider:0 add:0

		---------------------------------------------

		*/


		static readonly bool		debug = false;

		enum BaseGraphToken
		{
			Undefined,
			NewNodeCommand,
			LinkCommand,
			LinkAnchorCommand,
			GraphAttributeCommand,
			OpenParenthesis,
			ClosedParenthesis,
			Word,
			IntValue,
			FloatValue,
			BoolValue,
			Comma,
			Attr,
			Colon,
			Equal,
			JsonDatas,
		}

		class BaseGraphTokenMatch
		{
			public BaseGraphToken	token;
			public string		value;
			public string		remainingText;
		}

		class BaseGraphTokenDefinition
		{
			public Regex					regex;
			public readonly BaseGraphToken	token;

			public BaseGraphTokenDefinition(BaseGraphToken graphToken, string regexString)
			{
				this.token = graphToken;
				this.regex = new Regex(regexString);
			}

			public BaseGraphTokenMatch Match(string input)
			{
				Match				m = regex.Match(input);
				BaseGraphTokenMatch	ret = null;

				if (m.Success)
				{
					ret = new BaseGraphTokenMatch();

					ret.token = token;
					ret.value = m.Value.Trim();
					if (ret.value[0] == '"')
						ret.value = ret.value.Trim('"');
					ret.remainingText = input.Substring(m.Value.Length);
				}
				return ret;
			}
		}

		static class BaseGraphTokenSequence
		{
			public readonly static List< BaseGraphToken > newNode = new List< BaseGraphToken > {
				BaseGraphToken.NewNodeCommand, BaseGraphToken.Word, BaseGraphToken.Word
			};

			public readonly static List< BaseGraphToken > newNodePosition = new List< BaseGraphToken > {
				BaseGraphToken.NewNodeCommand, BaseGraphToken.Word, BaseGraphToken.Word, BaseGraphToken.OpenParenthesis, BaseGraphToken.IntValue, BaseGraphToken.Comma, BaseGraphToken.IntValue, BaseGraphToken.ClosedParenthesis
			};

			public readonly static List< BaseGraphToken > newLink = new List< BaseGraphToken > {
				BaseGraphToken.LinkCommand, BaseGraphToken.Word, BaseGraphToken.Word
			};

			public readonly static List< BaseGraphToken > newLinkAnchor = new List< BaseGraphToken > {
				BaseGraphToken.LinkAnchorCommand, BaseGraphToken.Word, BaseGraphToken.Colon, BaseGraphToken.IntValue, BaseGraphToken.Word, BaseGraphToken.Colon, BaseGraphToken.IntValue
			};
			
			public readonly static List< BaseGraphToken > newLinkAnchorName = new List< BaseGraphToken > {
				BaseGraphToken.LinkAnchorCommand, BaseGraphToken.Word, BaseGraphToken.Colon, BaseGraphToken.Word, BaseGraphToken.Word, BaseGraphToken.Colon, BaseGraphToken.Word
			};

			public readonly static List< BaseGraphToken > newNodeAttrOption = new List< BaseGraphToken > {
				BaseGraphToken.Attr, BaseGraphToken.Equal, BaseGraphToken.JsonDatas
			};

			public readonly static List< BaseGraphToken > graphAttribute = new List< BaseGraphToken > {
				BaseGraphToken.GraphAttributeCommand, BaseGraphToken.Word, BaseGraphToken.Word
			};
		}

		class BaseGraphCommandTokenSequence
		{
			public BaseGraphCommandType		type;
			public List< BaseGraphToken >	requiredTokens;
			public List< BaseGraphToken >	options;
		}

		static class BaseGraphValidCommandTokenSequence
		{
			public readonly static List< BaseGraphCommandTokenSequence > validSequences = new List< BaseGraphCommandTokenSequence >
			{
				//New Node position command
				new BaseGraphCommandTokenSequence {
					type = BaseGraphCommandType.NewNodePosition,
					requiredTokens = BaseGraphTokenSequence.newNodePosition,
					options = BaseGraphTokenSequence.newNodeAttrOption,
				},
				//New Node command
				new BaseGraphCommandTokenSequence {
					type = BaseGraphCommandType.NewNode,
					options = BaseGraphTokenSequence.newNodeAttrOption,
					requiredTokens = BaseGraphTokenSequence.newNode,
				},
				//New Link command
				new BaseGraphCommandTokenSequence {
					type = BaseGraphCommandType.Link,
					requiredTokens = BaseGraphTokenSequence.newLink,
				},
				//New Link Anchor command
				new BaseGraphCommandTokenSequence {
					type = BaseGraphCommandType.LinkAnchor,
					requiredTokens = BaseGraphTokenSequence.newLinkAnchor
				},
				//New Link Anchor using names command
				new BaseGraphCommandTokenSequence {
					type = BaseGraphCommandType.LinkAnchorName,
					requiredTokens = BaseGraphTokenSequence.newLinkAnchorName
				},
				//Graph field command
				new BaseGraphCommandTokenSequence {
					type = BaseGraphCommandType.GraphAttribute,
					requiredTokens = BaseGraphTokenSequence.graphAttribute,
				}
			};
		}

		//token regex list by priority order
		readonly static List< BaseGraphTokenDefinition >	tokenDefinitions = new List< BaseGraphTokenDefinition >
		{
			new BaseGraphTokenDefinition(BaseGraphToken.LinkAnchorCommand, @"^LinkAnchor"),
			new BaseGraphTokenDefinition(BaseGraphToken.LinkCommand, @"^Link"),
			new BaseGraphTokenDefinition(BaseGraphToken.NewNodeCommand, @"^NewNode"),
			new BaseGraphTokenDefinition(BaseGraphToken.GraphAttributeCommand, @"^GraphAttr"),
			new BaseGraphTokenDefinition(BaseGraphToken.OpenParenthesis, @"^\("),
			new BaseGraphTokenDefinition(BaseGraphToken.ClosedParenthesis, @"^\)"),
			new BaseGraphTokenDefinition(BaseGraphToken.IntValue, @"^[-+]?\d+"),
			new BaseGraphTokenDefinition(BaseGraphToken.FloatValue, @"^[+-]?([0-9]+([.][0-9]*)?|[.][0-9]+)"),
			new BaseGraphTokenDefinition(BaseGraphToken.BoolValue, @"^(true|false)"),
			new BaseGraphTokenDefinition(BaseGraphToken.Comma, @"^,"),
			new BaseGraphTokenDefinition(BaseGraphToken.Attr, @"^attr"),
			new BaseGraphTokenDefinition(BaseGraphToken.Equal, @"^="),
			new BaseGraphTokenDefinition(BaseGraphToken.Colon, @"^:"),
			new BaseGraphTokenDefinition(BaseGraphToken.JsonDatas, @"^{.*}"),
			new BaseGraphTokenDefinition(BaseGraphToken.Word, "^(\\\"(.*?)\\\"|\\S+)"),
		};

		static IEnumerable< BaseGraphTokenDefinition > GetTokenDefinitionsForStartToken(BaseGraphToken firstToken)
		{
			if (firstToken == BaseGraphToken.Undefined)
			{
				foreach (var tokenDef in tokenDefinitions)
					yield return tokenDef;
				yield break;
			}
			
			var seq = BaseGraphValidCommandTokenSequence.validSequences.FirstOrDefault(s => s.requiredTokens.First() == firstToken);
			List< BaseGraphToken > tokens = new List< BaseGraphToken >();

			foreach (var token in seq.requiredTokens)
				tokens.Add(token);
			if (seq.options != null)
				foreach (var token in seq.options)
					tokens.Add(token);
				
			foreach (var tokenDef in tokenDefinitions)
				if (tokens.Contains(tokenDef.token))
					yield return tokenDef;
		}

		//returns all possible token matches
		static BaseGraphTokenMatch	MatchTokens(string line, BaseGraphToken firstToken)
		{
			foreach (var tokenDef in GetTokenDefinitionsForStartToken(firstToken))
			{
				BaseGraphTokenMatch	ret = tokenDef.Match(line.Trim());

				if (ret != null)
				{
					if (debug)
						Debug.Log("Token " + ret.token + " maches value: |" + ret.value + "|, remainingText: |" + ret.remainingText + "|");
					return ret;
				}
			}

			if (debug)
				Debug.Log("No token maches found with line: " + line);
			
			return null;
		}

		static Type	TryParseNodeType(string type)
		{
			Type	nodeType;

			//try to parse the node type:
			nodeType = Type.GetType(type);
			if (nodeType == null)
				nodeType = Type.GetType("ProceduralWorlds.Nodes." + type);
			if (nodeType == null)
				nodeType = Type.GetType("ProceduralWorlds.Core." + type);

			//thorw exception if the type can't be parse / does not inherit from BaseNode
			if (nodeType == null || !nodeType.IsSubclassOf(typeof(BaseNode)))
				throw new InvalidOperationException("Type " + type + " not found as a node type (" + nodeType + ")");
			
			return nodeType;
		}

		static Vector2 TryParsePosition(string x, string y)
		{
			return new Vector2(Int32.Parse(x), Int32.Parse(y));
		}

		static object TryParseGraphAttr(string s)
		{
			object value = null;
			int vi;
			long vl;
			float vf;
			double vd;
			bool vb;

			if (int.TryParse(s, out vi))
				value = vi;
			else if (long.TryParse(s, out vl))
				value = vl;
			else if (float.TryParse(s, out vf))
				value = vf;
			else if (double.TryParse(s, out vd))
				value = vd;
			else if (bool.TryParse(s, out vb))
				value = vb;
			
			return value;
		}

		static BaseGraphCommand CreateGraphCommand(BaseGraphCommandTokenSequence seq, List< BaseGraphTokenMatch > tokens)
		{
			Type	nodeType;
			string	attributes = null;

			switch (seq.type)
			{
				case BaseGraphCommandType.Link:
					return new BaseGraphCommand(tokens[1].value, tokens[2].value);
				case BaseGraphCommandType.LinkAnchor:
					int fromAnchorField = int.Parse(tokens[3].value);
					int toAnchorField = int.Parse(tokens[6].value);
					return new BaseGraphCommand(tokens[1].value, fromAnchorField, tokens[4].value, toAnchorField);
				case BaseGraphCommandType.LinkAnchorName:
					return new BaseGraphCommand(tokens[1].value, tokens[3].value, tokens[4].value, tokens[6].value);
				case BaseGraphCommandType.NewNode:
					nodeType = TryParseNodeType(tokens[1].value);
					if (tokens.Count > 4)
						attributes = tokens[5].value;
					return new BaseGraphCommand(nodeType, tokens[2].value, attributes);
				case BaseGraphCommandType.NewNodePosition:
					nodeType = TryParseNodeType(tokens[1].value);
					if (tokens.Count > 9)
						attributes = tokens[10].value;
					Vector2 position = TryParsePosition(tokens[4].value, tokens[6].value);
					return new BaseGraphCommand(nodeType, tokens[2].value, position, attributes);
				case BaseGraphCommandType.GraphAttribute:
					object value = TryParseGraphAttr(tokens[2].value);
					
					if (value == null)
						return null;
					
					return new BaseGraphCommand(tokens[1].value, value);
				default:
					return null;
			}
		}

		static BaseGraphCommand		BuildCommand(List< BaseGraphTokenMatch > tokens, string startLine)
		{
			foreach (var validTokenList in BaseGraphValidCommandTokenSequence.validSequences)
			{
				//check if the token count can match the current valid token list
				if (validTokenList.requiredTokens.Count > tokens.Count)
					continue ;

				int i = 0;
				
				//check if the tokens we received correspond to a valid squence of token
				for (i = 0; i < validTokenList.requiredTokens.Count; i++)
				{
					if (tokens[i].token != validTokenList.requiredTokens[i])
						goto skipLoop;
				}

				//if the validTokenList does not take options but there are remaining tokens, skip this command:
				if (validTokenList.options == null && i < tokens.Count)
					goto skipLoop;

				//check for options:
				for (int j = 0; i < tokens.Count; i++, j++)
				{
					if (tokens[i].token != validTokenList.options[j])
						goto skipLoop;
				}
				
				//the valid token list iterated until it's end so we have a valid command
				return CreateGraphCommand(validTokenList, tokens);

				skipLoop:
				continue ;
			}

			throw new InvalidOperationException("Invalid token squence: " + startLine);
		}

		public static BaseGraphCommand	Parse(string inputCommand)
		{
			List< BaseGraphTokenMatch >	lineTokens = new List< BaseGraphTokenMatch >();
			string						startLine = inputCommand;
			BaseGraphToken				firstToken = BaseGraphToken.Undefined;
			bool						first = true;

			while (!String.IsNullOrEmpty(inputCommand))
			{
				var match = MatchTokens(inputCommand, firstToken);

				if (match == null)
					throw new InvalidOperationException("Invalid token at line \"" + inputCommand + "\"");

				inputCommand = match.remainingText;

				if (first)
					firstToken = match.token;
				
				lineTokens.Add(match);
				first = false;
			}

			if (lineTokens.Count == 0)
				throw new InvalidOperationException("Invalid empty command: " + inputCommand);
			
			return BuildCommand(lineTokens, startLine);
		}

	}
}