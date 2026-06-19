using System;
using System.Collections.Generic;
using System.Reflection;

namespace PFGraph
{
    public static class ViewModelFactory
    {
        private static bool initialized;
        private static Dictionary<Type, IViewModelProducer> viewModelProducers;

        static ViewModelFactory()
        {
            Init(true);
        }

        public static void Init(bool force)
        {
            if (!force && initialized)
                return;

            if (viewModelProducers == null)
            {
                viewModelProducers = new Dictionary<Type, IViewModelProducer>();
            }
            else
            {
                viewModelProducers.Clear();
            }

            foreach (var type in TypesCache.GetTypesWithAttribute<ViewModelAttribute>())
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                if (type.IsGenericType)
                {
                    continue;
                }

                var attribute = CustomAttributeExtensions.GetCustomAttribute<ViewModelAttribute>(type, true);
                if (HasDefaultConstructor(type))
                {
                    var producerType = typeof(ViewModelProducerS<>).MakeGenericType(type);
                    viewModelProducers.Add(attribute.ModelType, Activator.CreateInstance(producerType) as IViewModelProducer);
                }
                else
                {
                    var producerType = typeof(ViewModelProducerR<>).MakeGenericType(type);
                    viewModelProducers.Add(attribute.ModelType, Activator.CreateInstance(producerType) as IViewModelProducer);
                }
            }

            initialized = true;
        }

        private static bool HasDefaultConstructor(Type type)
        {
            return type == typeof(string) || type.IsArray || type.IsValueType || type.GetConstructor(System.Type.EmptyTypes) != (ConstructorInfo)null;
        }

        private static IViewModelProducer GetProducer(Type modelType)
        {
            if (!viewModelProducers.TryGetValue(modelType, out var producer))
            {
                var type = modelType;
                do
                {
                    type = type.BaseType;
                } while (type != null && !viewModelProducers.TryGetValue(type, out producer));

                viewModelProducers[modelType] = producer;
            }

            return producer;
        }

        public static Type GetViewModelType(Type modelType)
        {
            return GetProducer(modelType)?.ViewModelType;
        }

        public static ViewModel ProduceViewModel(object model)
        {
            var modelType = model.GetType();
            var producer = GetProducer(modelType);
            if (producer != null)
            {
                return Activator.CreateInstance(producer.ViewModelType, model) as ViewModel;
            }

            return null;
        }

        public static object ProduceViewModel(Type modelType)
        {
            var producer = GetProducer(modelType);
            if (producer != null)
            {
                return producer.Produce();
            }

            return null;
        }

        public static object ProduceViewModel<TModel>()
        {
            var producer = GetProducer(TypeCache<TModel>.TYPE);
            if (producer != null)
            {
                return producer.Produce();
            }

            return null;
        }
    }

    public interface IViewModelProducer
    {
        Type ViewModelType { get; }

        object Produce();
    }

    public sealed class ViewModelProducerR<T> : IViewModelProducer where T : class
    {
        public Type ViewModelType => typeof(T);

        public T Produce() => Activator.CreateInstance<T>();

        object IViewModelProducer.Produce() => Produce();
    }

    public sealed class ViewModelProducerS<T> : IViewModelProducer where T : class, new()
    {
        public Type ViewModelType => typeof(T);

        public T Produce() => new T();

        object IViewModelProducer.Produce() => Produce();
    }
}