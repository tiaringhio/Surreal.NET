using System.Text.Json;

namespace SurrealDB.Ws.Models;

public readonly record struct ResponseHeader(string? id, bool async, string? method) {
    public bool IsDefault => default == this;

    /// <summary>
    /// Parses the head including the result propertyname, excluding the result array.
    /// </summary>
    internal static (ResponseHeader head, long off, string? err) Parse(in ReadOnlySpan<byte> utf8) {
        Fsm fsm = new() {
            Lexer = new(utf8, false, new JsonReaderState(new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })),
            State =  Fsms.Start,
        };
        while (fsm.MoveNext()) {}

        if (!fsm.Success) {
            return (default, fsm.Lexer.BytesConsumed, $"Error while parsing {nameof(ResponseHeader)} at {fsm.Lexer.TokenStartIndex}: {fsm.Err}");
        }
        return (new(fsm.Id, fsm.Async, fsm.Method), default, default);
    }

    private enum Fsms {
        Start, // -> Prop
        Prop, // -> PropId | PropAsync | PropMethod | ProsResult
        PropId, // -> Prop | End
        PropAsync, // -> Prop | End
        PropMethod, // -> Prop | End
        PropResult, // -> End
        End
    }

    private ref struct Fsm {
        public Fsms State;
        public Utf8JsonReader Lexer;
        public string? Err;
        public bool Success;

        public string? Name;
        public string? Id;
        public bool Async;
        public string? Method;

        public bool MoveNext() {
            return State switch {
                Fsms.Start => Start(),
                Fsms.Prop => Prop(),
                Fsms.PropId => PropId(),
                Fsms.PropAsync => PropAsync(),
                Fsms.PropMethod => PropMethod(),
                Fsms.PropResult => PropResult(),
                Fsms.End => End(),
                _ => false
            };
        }

        private bool Start() {
            if (!Lexer.Read() || Lexer.TokenType != JsonTokenType.StartObject) {
                Err = "Unable to read token StartObject";
                return false;
            }

            State = Fsms.Prop;
            return true;

        }

        private bool End() {
            Success = !String.IsNullOrEmpty(Id) && !String.IsNullOrEmpty(Method);
            return false;
        }

        private bool Prop() {
            if (!Lexer.Read() || Lexer.TokenType != JsonTokenType.PropertyName) {
                Err = "Unable to read PropertyName";
                return false;
            }

            Name = Lexer.GetString();
            if ("id".Equals(Name, StringComparison.OrdinalIgnoreCase)) {
                State = Fsms.PropId;
                return true;
            }
            if ("async".Equals(Name, StringComparison.OrdinalIgnoreCase)) {
                State = Fsms.PropAsync;
                return true;
            }
            if ("method".Equals(Name, StringComparison.OrdinalIgnoreCase)) {
                State = Fsms.PropMethod;
                return true;
            }
            if ("result".Equals(Name, StringComparison.OrdinalIgnoreCase)) {
                State = Fsms.PropResult;
                return true;
            }

            Err = $"Unknown PropertyName `{Name}`";
            return false;
        }

        private bool PropId() {
            if (!Lexer.Read() || Lexer.TokenType != JsonTokenType.String) {
                Err = "Unable to read `id` property value";
                return false;
            }

            State = Fsms.Prop;
            Id = Lexer.GetString();
            return true;
        }

        private bool PropAsync() {
            if (!Lexer.Read() || Lexer.TokenType is not JsonTokenType.True and not JsonTokenType.False) {
                Err = "Unable to read `async` property value";
                return false;
            }

            State = Fsms.Prop;
            Async = Lexer.GetBoolean();
            return true;
        }
        private bool PropMethod() {
            if (!Lexer.Read() || Lexer.TokenType != JsonTokenType.String) {
                Err = "Unable to read `method` property value";
                return false;
            }

            State = Fsms.Prop;
            Method = Lexer.GetString();
            return true;
        }

        private bool PropResult() {
            // Do not parse the result!
            // The complete result is not present in the buffer!
            // The result is returned as a unevaluated asynchronous stream!
            State = Fsms.End;
            return true;
        }
    }
}
