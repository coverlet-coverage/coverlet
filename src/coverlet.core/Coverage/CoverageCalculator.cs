using System.Collections.Generic;
using System.IO;
using System.Linq;

using Coverlet.Core.Helpers;
using Coverlet.Core.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Coverlet.Core
{
    public class CoverageCalculator : ICoverageCalculator
    {
        private readonly InstrumenterState _instrumenterState;
        private readonly ILogger _logger;

        public CoverageCalculator(InstrumenterState instrumenterState, ILogger logger) => (_instrumenterState, _logger) = (instrumenterState, logger);

        public CoverageResult GetCoverageResult()
        {
            CalculateCoverage();

            Modules modules = new Modules();
            foreach (InstrumenterResult result in _instrumenterState.InstrumenterResults)
            {
                Documents documents = new Documents();
                foreach (var doc in result.Documents.Values)
                {
                    // Construct Line Results
                    foreach (var line in doc.Lines.Values)
                    {
                        if (documents.TryGetValue(doc.Path, out Classes classes))
                        {
                            if (classes.TryGetValue(line.Class, out Methods methods))
                            {
                                if (methods.TryGetValue(line.Method, out Method method))
                                {
                                    documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, line.Hits);
                                }
                                else
                                {
                                    documents[doc.Path][line.Class].Add(line.Method, new Method());
                                    documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, line.Hits);
                                }
                            }
                            else
                            {
                                documents[doc.Path].Add(line.Class, new Methods());
                                documents[doc.Path][line.Class].Add(line.Method, new Method());
                                documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, line.Hits);
                            }
                        }
                        else
                        {
                            documents.Add(doc.Path, new Classes());
                            documents[doc.Path].Add(line.Class, new Methods());
                            documents[doc.Path][line.Class].Add(line.Method, new Method());
                            documents[doc.Path][line.Class][line.Method].Lines.Add(line.Number, line.Hits);
                        }
                    }

                    // Construct Branch Results
                    foreach (var branch in doc.Branches.Values)
                    {
                        if (documents.TryGetValue(doc.Path, out Classes classes))
                        {
                            if (classes.TryGetValue(branch.Class, out Methods methods))
                            {
                                if (methods.TryGetValue(branch.Method, out Method method))
                                {
                                    method.Branches.Add(new BranchInfo
                                    { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                                    );
                                }
                                else
                                {
                                    documents[doc.Path][branch.Class].Add(branch.Method, new Method());
                                    documents[doc.Path][branch.Class][branch.Method].Branches.Add(new BranchInfo
                                    { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                                    );
                                }
                            }
                            else
                            {
                                documents[doc.Path].Add(branch.Class, new Methods());
                                documents[doc.Path][branch.Class].Add(branch.Method, new Method());
                                documents[doc.Path][branch.Class][branch.Method].Branches.Add(new BranchInfo
                                { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                                );
                            }
                        }
                        else
                        {
                            documents.Add(doc.Path, new Classes());
                            documents[doc.Path].Add(branch.Class, new Methods());
                            documents[doc.Path][branch.Class].Add(branch.Method, new Method());
                            documents[doc.Path][branch.Class][branch.Method].Branches.Add(new BranchInfo
                            { Line = branch.Number, Hits = branch.Hits, Offset = branch.Offset, EndOffset = branch.EndOffset, Path = branch.Path, Ordinal = branch.Ordinal }
                            );
                        }
                    }
                }

                modules.Add(Path.GetFileName(result.ModulePath), documents);
                InstrumentationHelper.RestoreOriginalModule(result.ModulePath, _instrumenterState.Identifier);
            }

            var coverageResult = new CoverageResult { Identifier = _instrumenterState.Identifier, Modules = modules, InstrumentedResults = _instrumenterState.InstrumenterResults.ToList() };

            if (!string.IsNullOrEmpty(_instrumenterState.MergeWith) && !string.IsNullOrWhiteSpace(_instrumenterState.MergeWith) && File.Exists(_instrumenterState.MergeWith))
            {
                string json = File.ReadAllText(_instrumenterState.MergeWith);
                coverageResult.Merge(JsonConvert.DeserializeObject<Modules>(json));
            }

            return coverageResult;
        }

        private void CalculateCoverage()
        {
            foreach (var result in _instrumenterState.InstrumenterResults)
            {
                if (!File.Exists(result.HitsFilePath))
                {
                    _logger.LogWarning($"Hits file:'{result.HitsFilePath}' not found for module: '{result.Module}'");
                    continue;
                }

                List<Document> documents = result.Documents.Values.ToList();
                if (_instrumenterState.UseSourceLink && result.SourceLink != null)
                {
                    var jObject = JObject.Parse(result.SourceLink)["documents"];
                    var sourceLinkDocuments = JsonConvert.DeserializeObject<Dictionary<string, string>>(jObject.ToString());
                    foreach (var document in documents)
                    {
                        document.Path = GetSourceLinkUrl(sourceLinkDocuments, document.Path);
                    }
                }

                using (var fs = new FileStream(result.HitsFilePath, FileMode.Open))
                using (var br = new BinaryReader(fs))
                {
                    int hitCandidatesCount = br.ReadInt32();

                    // TODO: hitCandidatesCount should be verified against result.HitCandidates.Count

                    var documentsList = result.Documents.Values.ToList();

                    for (int i = 0; i < hitCandidatesCount; ++i)
                    {
                        var hitLocation = result.HitCandidates[i];
                        var document = documentsList[hitLocation.DocIndex];
                        int hits = br.ReadInt32();

                        if (hitLocation.IsBranch)
                        {
                            var branch = document.Branches[new BranchKey(hitLocation.Start, hitLocation.End)];
                            branch.Hits += hits;
                        }
                        else
                        {
                            for (int j = hitLocation.Start; j <= hitLocation.End; j++)
                            {
                                var line = document.Lines[j];
                                line.Hits += hits;
                            }
                        }
                    }
                }

                // for MoveNext() compiler autogenerated method we need to patch false positive (IAsyncStateMachine for instance) 
                // we'll remove all MoveNext() not covered branch
                foreach (var document in result.Documents)
                {
                    List<KeyValuePair<BranchKey, Branch>> branchesToRemove = new List<KeyValuePair<BranchKey, Branch>>();
                    foreach (var branch in document.Value.Branches)
                    {
                        //if one branch is covered we search the other one only if it's not covered
                        if (IsAsyncStateMachineMethod(branch.Value.Method) && branch.Value.Hits > 0)
                        {
                            foreach (var moveNextBranch in document.Value.Branches)
                            {
                                if (moveNextBranch.Value.Method == branch.Value.Method && moveNextBranch.Value != branch.Value && moveNextBranch.Value.Hits == 0)
                                {
                                    branchesToRemove.Add(moveNextBranch);
                                }
                            }
                        }
                    }
                    foreach (var branchToRemove in branchesToRemove)
                    {
                        document.Value.Branches.Remove(branchToRemove.Key);
                    }
                }

                InstrumentationHelper.DeleteHitsFile(result.HitsFilePath);
            }
        }

        private bool IsAsyncStateMachineMethod(string method)
        {
            if (!method.EndsWith("::MoveNext()"))
            {
                return false;
            }

            foreach (var instrumentationResult in _instrumenterState.InstrumenterResults)
            {
                if (instrumentationResult.AsyncMachineStateMethod.Contains(method))
                {
                    return true;
                }
            }
            return false;
        }

        private string GetSourceLinkUrl(Dictionary<string, string> sourceLinkDocuments, string document)
        {
            if (sourceLinkDocuments.TryGetValue(document, out string url))
            {
                return url;
            }

            var keyWithBestMatch = string.Empty;
            var relativePathOfBestMatch = string.Empty;

            foreach (var sourceLinkDocument in sourceLinkDocuments)
            {
                string key = sourceLinkDocument.Key;
                if (Path.GetFileName(key) != "*") continue;

                if (!Path.GetDirectoryName(document).StartsWith(Path.GetDirectoryName(key) + Path.DirectorySeparatorChar))
                    continue;

                var relativePath = Path.GetDirectoryName(document).Substring(Path.GetDirectoryName(key).Length + 1);

                if (relativePathOfBestMatch.Length == 0)
                {
                    keyWithBestMatch = sourceLinkDocument.Key;
                    relativePathOfBestMatch = relativePath;
                }

                if (relativePath.Length < relativePathOfBestMatch.Length)
                {
                    keyWithBestMatch = sourceLinkDocument.Key;
                    relativePathOfBestMatch = relativePath;
                }
            }

            relativePathOfBestMatch = relativePathOfBestMatch == "." ? string.Empty : relativePathOfBestMatch;

            string replacement = Path.Combine(relativePathOfBestMatch, Path.GetFileName(document));
            replacement = replacement.Replace('\\', '/');

            url = sourceLinkDocuments[keyWithBestMatch];
            return url.Replace("*", replacement);
        }
    }
}
