using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.JsonPatch;

namespace Stardust.Interstellar.Rest.JsonPatch
{
    public class PatchableDocumentBase<T> : IPatchableDocument where T : class
    {
        public JsonPatchDocument<T> AsJsonPatch()
        {
            return _patchDocument;
        }

        protected T _document;
        protected JsonPatchDocument<T> _patchDocument;

        public PatchableDocumentBase()
        {

        }
        public PatchableDocumentBase(T document)
        {
            _document = document;
            _patchDocument = new JsonPatchDocument<T>();

        }
        protected bool SetReferenceType<TProp>(TProp value, Expression<Func<T, TProp>> path)
        {

            var oldValue = path.Compile().Invoke(_document);
            if (Equals(oldValue, value)) return false;
            if (oldValue == null)
                _patchDocument.Add(path, value);
            else if (value == null)
                _patchDocument.Remove(path);
            else
                _patchDocument.Replace(path, value);
            return true;

        }
    }
}