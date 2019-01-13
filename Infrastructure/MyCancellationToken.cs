using System;

namespace VeemExercise.Infrastructure
{
    public class MyCancellationToken
    {
        private bool _isCancelled;
        public bool IsCancelled => _isCancelled;
        public event Action OnCancel;
        
        public void Cancel()
        {
            _isCancelled = true;
            OnCancel?.Invoke();
        }
    }
}
