﻿using System;
using System.Diagnostics;
using Moq;

namespace Nest.Indexify.Tests
{
    public class ElasticClientContext : IDisposable
    {
        public ICreateIndexRequest IndexCreateRequest { get; private set; }
        
        public Mock<IElasticClient> Client { get; }

        public Mock<IConnectionSettingsValues> Settings { get; }

        public ElasticClientContext()
        {
            Debug.WriteLine("Setup ElasticClientContext");
            Settings = new Mock<IConnectionSettingsValues>();
            Client = SetupClientCore(new Mock<IElasticClient>());
        }

        private Mock<IElasticClient> SetupClientCore(Mock<IElasticClient> clientMock)
        {
            clientMock.SetupGet(s => s.Infer).Returns(new ElasticInferrer(Settings.Object));
            clientMock.Setup(s => s.CreateIndex(It.IsAny<Func<CreateIndexDescriptor, CreateIndexDescriptor>>())).Callback((Func<CreateIndexDescriptor, CreateIndexDescriptor> cb) =>
            {
                IndexCreateRequest = cb(new CreateIndexDescriptor(Settings.Object));
            }).Returns(new IndicesOperationResponse());

            clientMock.Setup(s => s.CreateIndexAsync(It.IsAny<Func<CreateIndexDescriptor, CreateIndexDescriptor>>())).Callback((Func<CreateIndexDescriptor, CreateIndexDescriptor> cb) =>
            {
                IndexCreateRequest = cb(new CreateIndexDescriptor(Settings.Object));
            }).ReturnsAsync(new IndicesOperationResponse());

            return SetupClient(clientMock);
        }

        protected virtual Mock<IElasticClient> SetupClient(Mock<IElasticClient> clientMock)
        {
            return clientMock;
        }

        public void Dispose()
        {
            Debug.WriteLine("Dispose ElasticClientContext ({0})", GetHashCode());
        }
    }
}