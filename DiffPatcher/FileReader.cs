﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Yggdrasil.IO
{
	/// <summary>
	/// General use class to read files line for line.
	/// </summary>
	/// <remarks>
	/// Ignores empty lines and lines prefixed with '!', ';', '#', '//', or '--'.
	/// Supports including of files via 'include {file}' and 'require {file}'.
	/// Require raises an exception if the file couldn't be found.
	/// 
	/// Paths to included files may not be absolute. If a path starts with
	/// a slash, the path is relative to the working directory.
	/// This simplifies including files from other sub-folders of the
	/// application.
	/// </remarks>
	/// <example>
	/// <code>
	///	using (var fr = new FileReader(filePath))
	///	{
	///		foreach (string line in fr)
	///		{
	///			// Do something with line
	///		}
	///	}
	///	</code>
	///	</example>
	public class FileReader : IEnumerable<FileReaderLine>, IDisposable
	{
		private string _filePath;
		private string _relativePath;
		private StreamReader _streamReader;

		/// <summary>
		/// Returns the line the reader is currently on.
		/// </summary>
		public int CurrentLine { get; protected set; }

		/// <summary>
		/// Creates new reader for given file.
		/// </summary>
		/// <param name="filePath"></param>
		public FileReader(string filePath)
		{
			if (!File.Exists(filePath))
				throw new FileNotFoundException("File '" + filePath + "' not found.");

			_filePath = filePath;
			_relativePath = Path.GetDirectoryName(Path.GetFullPath(filePath));

			_streamReader = new StreamReader(filePath);
		}

		/// <summary>
		/// Returns enumerator for the lines in the open file.
		/// </summary>
		/// <returns></returns>
		public IEnumerator<FileReaderLine> GetEnumerator()
		{
			string line;

			// Until EOF
			while ((line = _streamReader.ReadLine()) != null)
			{
				this.CurrentLine++;

				line = line.Trim();

				if (string.IsNullOrWhiteSpace(line))
					continue;

				// Ignore very short or commented lines
				if (line.Length < 2 || line[0] == '!' || line[0] == ';' || line[0] == '#' || line.StartsWith("//") || line.StartsWith("--"))
					continue;

				// Include files
				bool require = false, divert = false;
				if (line.StartsWith("include ") || (require = line.StartsWith("require ")) || (divert = line.StartsWith("divert ")))
				{
					var fileName = line.Substring(line.IndexOf(' ')).Trim(' ', '"');
					var includeFilePath = Path.Combine((!fileName.StartsWith("/") ? _relativePath : ""), fileName.TrimStart('/'));

					// Prevent rekursive including
					if (includeFilePath != _filePath)
					{
						// Silently ignore failed includes, only raise an
						// exception on require.
						if (File.Exists(includeFilePath))
						{
							using (var fr = new FileReader(includeFilePath))
							{
								foreach (var incLine in fr)
									yield return incLine;
							}

							// Stop reading current file if divert was successful
							if (divert)
								yield break;
						}
						else if (require)
						{
							throw new FileNotFoundException("Required file '" + includeFilePath + "' not found.");
						}
					}

					continue;
				}

				yield return new FileReaderLine(line, _filePath);
			}
		}

		/// <summary>
		/// Returns enumerator for the lines in the open file.
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Closes internal stream reader.
		/// </summary>
		public void Dispose()
		{
			_streamReader.Close();
		}
	}

	/// <summary>
	/// Represents a line read from a file by the FileReader.
	/// </summary>
	public class FileReaderLine
	{
		/// <summary>
		/// Current line.
		/// </summary>
		public string Value { get; private set; }

		/// <summary>
		/// Full path to the file the value was read from.
		/// </summary>
		public string File { get; private set; }

		/// <summary>
		/// New FileReaderLine.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="file"></param>
		public FileReaderLine(string line, string file)
		{
			this.Value = line;
			this.File = Path.GetFullPath(file);
		}
	}
}
