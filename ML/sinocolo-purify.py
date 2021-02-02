from nn import NNTraining

training = NNTraining(inputFile='sino-ml-purify.json', outputFile='sino-ml-purify.txt')
training.run(numHidden1=64, numEpochs=40)
