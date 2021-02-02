from nn import NNTraining

training = NNTraining(inputFile='sino-ml-stats.json', outputFile='sino-ml-stats.txt')
training.run(numHidden1=256, numEpochs=50)
